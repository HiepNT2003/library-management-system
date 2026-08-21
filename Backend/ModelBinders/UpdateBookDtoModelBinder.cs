using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public class UpdateBookDtoModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var request = bindingContext.HttpContext.Request;

        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        var jsonDoc = JsonDocument.Parse(body);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("documentTypeId", out var typeProp))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        var type = typeProp.GetInt32();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        BaseUpdateBookDto dto = type switch
        {
            1 => JsonSerializer.Deserialize<UpdatePhysicalBookDto>(body, options)!,
            2 => JsonSerializer.Deserialize<UpdateArticleDto>(body, options)!,
            3 => JsonSerializer.Deserialize<UpdateThesisDto>(body, options)!,
            4 => JsonSerializer.Deserialize<UpdateEbookDto>(body, options)!,
            _ => throw new Exception("Invalid document type")
        };

        bindingContext.Result = ModelBindingResult.Success(dto);
    }
}