using System.Diagnostics.Contracts;

namespace SharpBot.Systems.Extensions;

public static class HttpRequestMessage_Extensions {
    /// <summary>
    ///     Clones the current <see cref="HttpRequestMessage" /> into a new instance.
    /// </summary>
    /// <param name="current"> The instance to clone </param>
    /// <returns> A new instance that is a clone of <paramref name="current" />. </returns>
    [Pure]
    public static async Task<HttpRequestMessage> Clone(this HttpRequestMessage req) {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        // Copy the request's content (via a MemoryStream) into the cloned object
        var ms = new MemoryStream();
        if (req.Content != null) {
            await req.Content.CopyToAsync(ms);


            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            // Copy the content headers
            foreach (var h in req.Content.Headers)
                clone.Content.Headers.Add(h.Key, h.Value);
        }


        clone.Version = req.Version;

        foreach (var option in req.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        foreach (var (key, value) in req.Headers)
            clone.Headers.TryAddWithoutValidation(key, value);

        req.Dispose();

        return clone;
    }
}