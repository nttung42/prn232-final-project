using System.Collections;
using System.Reflection;
using System.Text;
using BadmintonBooking.BLL.DTOs.Bookings;
using BadmintonBooking.BLL.DTOs.Common;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace BadmintonBooking.WebAPI.Formatters;

public class CsvOutputFormatter : TextOutputFormatter
{
    public CsvOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanWriteType(Type? type)
    {
        if (type == null) return false;
        
        return typeof(IEnumerable).IsAssignableFrom(type) ||
               type == typeof(BookingDto) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>));
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var response = context.HttpContext.Response;
        var buffer = new StringBuilder();

        object? data = context.Object;

        // If wrapped in ApiResponse<T>, extract Data
        if (data != null && data.GetType().IsGenericType && data.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            var dataProp = data.GetType().GetProperty("Data");
            data = dataProp?.GetValue(data);
        }

        if (data is IEnumerable enumerable && !(data is string))
        {
            var list = enumerable.Cast<object>().ToList();
            if (list.Count > 0)
            {
                var itemType = list[0].GetType();
                var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // CSV Header
                buffer.AppendLine(string.Join(",", properties.Select(p => EscapeCsvValue(p.Name))));

                // CSV Rows
                foreach (var item in list)
                {
                    var values = properties.Select(p => EscapeCsvValue(p.GetValue(item)?.ToString() ?? string.Empty));
                    buffer.AppendLine(string.Join(",", values));
                }
            }
        }
        else if (data != null)
        {
            var itemType = data.GetType();
            var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            buffer.AppendLine(string.Join(",", properties.Select(p => EscapeCsvValue(p.Name))));
            var values = properties.Select(p => EscapeCsvValue(p.GetValue(data)?.ToString() ?? string.Empty));
            buffer.AppendLine(string.Join(",", values));
        }

        await response.WriteAsync(buffer.ToString(), selectedEncoding);
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return $"\"{value}\"";
    }
}

