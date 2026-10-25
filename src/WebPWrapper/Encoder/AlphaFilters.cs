namespace WebPWrapper.Encoder {
    /// <summary>Filtering strategies for the WebP alpha plane.</summary>
    public enum AlphaFilters {
        /// <summary>Do not filter the alpha plane.</summary>
        None,
        /// <summary>Use a fast filtering strategy.</summary>
        Fast,
        /// <summary>Test available filters and choose the smallest result.</summary>
        Best
    }
}