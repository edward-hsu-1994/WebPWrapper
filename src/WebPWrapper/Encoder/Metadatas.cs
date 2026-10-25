namespace WebPWrapper.Encoder {
    /// <summary>Metadata categories that can be copied during encoding.</summary>
    public enum Metadatas {
        /// <summary>Copy all supported metadata.</summary>
        All,
        /// <summary>Copy EXIF metadata.</summary>
        EXIF,
        /// <summary>Copy ICC color profile metadata.</summary>
        ICC,
        /// <summary>Copy XMP metadata.</summary>
        XMP
    }
}