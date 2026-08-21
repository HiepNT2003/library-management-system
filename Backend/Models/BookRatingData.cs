using Microsoft.ML.Data;

namespace Backend.Models;

// Input cho training
public class BookRatingData
{
    [KeyType(count: 100000)]
    public uint UserId { get; set; }

    [KeyType(count: 100000)]
    public uint BookId { get; set; }

    public float Label { get; set; } // số lần mượn = implicit rating
}

// Output sau khi predict
public class BookRatingPrediction
{
    public float Label { get; set; }
    public float Score { get; set; }
}