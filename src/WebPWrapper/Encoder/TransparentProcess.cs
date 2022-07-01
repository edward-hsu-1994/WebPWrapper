namespace WebPWrapper.Encoder {
    public enum TransparentProcesses {
        /// <summary>
        /// Preserve RGB values in transparent area.
        /// </summary>
        Exact,
        /// <summary>
        /// Using this option will discard the alpha channel.
        /// </summary>
        Remove,
        /// <summary>
        /// This option blends the alpha channel (if present).
        /// </summary>
        Blend
    }
}