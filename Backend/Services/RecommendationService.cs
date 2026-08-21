using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace Backend.Services;

public interface IRecommendationService
{
    Task TrainAsync();
    List<(int BookId, float Score)> Predict(string userId, int topN = 10);
    bool IsModelTrained { get; }
    void LoadIfExists();
}

public class RecommendationService : IRecommendationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecommendationService> _logger;
    private readonly MLContext _mlContext;

    private ITransformer? _model;
    private PredictionEngine<BookRatingData, BookRatingPrediction>? _predictionEngine;
    private Dictionary<string, uint> _userIdMap  = new();
    private Dictionary<int, uint>    _bookIdMap   = new();
    private List<int> _allBookIds = new();

    public bool IsModelTrained => _model != null;

    public RecommendationService(IServiceProvider serviceProvider, ILogger<RecommendationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
        _mlContext       = new MLContext(seed: 42);
    }
    private readonly string _modelPath = "recommendation_model.zip";

    public async Task TrainAsync()
    {
        _logger.LogInformation("Training recommendation model...");

        using var scope   = _serviceProvider.CreateScope();
        var context       = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Lấy dữ liệu mượn sách
        var borrowData = await context.Transactions
            .Include(t => t.Copy)
            .Where(t => t.Copy != null && t.Copy.BookId != null)
            .GroupBy(t => new { t.UserId, BookId = t.Copy!.BookId!.Value })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.BookId,
                Count = g.Count()
            })
            .ToListAsync();
        
        var borrowScores = borrowData.Select(t => new
        {
            t.UserId,
            t.BookId,
            Score = (float)Math.Min(t.Count, 5)
        }).ToList();

        var favoriteData = await context.UserFavoriteBooks
            .Where(f => f.BookId != null && f.UserId != null)
            .Select(f => new
            {
                UserId = f.UserId,
                BookId = f.BookId!.Value,
            })
            .ToListAsync();

        var favoriteScores = favoriteData.Select(f => new
        {
            f.UserId,
            f.BookId,
            Score = 3f
        }).ToList();

        var merged = borrowScores
            .Concat(favoriteScores)
            .GroupBy(x => new { x.UserId, x.BookId })
            .Select(g => new
            {
                UserId = g.Key.UserId,
                BookId = g.Key.BookId,
                Score  = Math.Min(g.Sum(x => x.Score), 5f)
            })
            .ToList();

        // Map userId và bookId
        var userIds = merged.Select(t => t.UserId).Distinct().ToList();
        var bookIds = merged.Select(t => t.BookId).Distinct().ToList();

        _userIdMap  = userIds.Select((id, idx) => (id, (uint)(idx + 1))).ToDictionary(x => x.id, x => x.Item2);
        _bookIdMap  = bookIds.Select((id, idx) => (id, (uint)(idx + 1))).ToDictionary(x => x.id, x => x.Item2);
        _allBookIds = bookIds;

        // Tạo training data
        var trainingData = merged.Select(t => new BookRatingData
        {
            UserId = _userIdMap[t.UserId],
            BookId = _bookIdMap[t.BookId],
            Label  = t.Score
        }).ToList();

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Train model Matrix Factorization
        var options = new MatrixFactorizationTrainer.Options
        {
            MatrixColumnIndexColumnName = nameof(BookRatingData.UserId),
            MatrixRowIndexColumnName    = nameof(BookRatingData.BookId),
            LabelColumnName             = nameof(BookRatingData.Label),
            NumberOfIterations          = 20,
            ApproximationRank           = 32,
            LearningRate                = 0.1,
        };

        var pipeline = _mlContext.Recommendation().Trainers.MatrixFactorization(options);
        _model       = pipeline.Fit(dataView);

        _mlContext.Model.Save(_model, dataView.Schema, _modelPath);
        _logger.LogInformation("Model saved to {Path}", _modelPath);

        _predictionEngine = _mlContext.Model.CreatePredictionEngine<BookRatingData, BookRatingPrediction>(_model);

        _logger.LogInformation("Recommendation model trained successfully with {Count} records.", trainingData.Count);
    }

    public List<(int BookId, float Score)> Predict(string userId, int topN = 10)
    {
        if (_predictionEngine == null || !_userIdMap.ContainsKey(userId))
            return new List<(int, float)>();

        var userUint = _userIdMap[userId];

        // Predict score cho tất cả sách
        var scores = new List<(int BookId, float Score)>();

        foreach (var (bookId, bookUint) in _bookIdMap)
        {
            var prediction = _predictionEngine.Predict(new BookRatingData
            {
                UserId = userUint,
                BookId = bookUint
            });
            scores.Add((bookId, prediction.Score));
        }

        return scores
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .ToList();
    }

    public void LoadIfExists()
    {
        if (!File.Exists(_modelPath)) return;
        try
        {
            _model = _mlContext.Model.Load(_modelPath, out _);
            _predictionEngine = _mlContext.Model
                .CreatePredictionEngine<BookRatingData, BookRatingPrediction>(_model);
            _logger.LogInformation("Loaded existing model from {Path}", _modelPath);
        }
        catch { _model = null; }
    }
}