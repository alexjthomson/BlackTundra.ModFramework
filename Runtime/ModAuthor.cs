namespace BlackTundra.ModFramework {

    /// <summary>
    /// Describes an author of a mod.
    /// </summary>
    public struct ModAuthor {

        #region variable

        /// <summary>
        /// Name of the author.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// Email of the author.
        /// </summary>
        public readonly string email;

        /// <summary>
        /// URL of the author.
        /// </summary>
        public readonly string url;

        #endregion

        #region constructor

        internal ModAuthor(in string name, in string email, in string url) {
            this.name = name;
            this.email = email;
            this.url = url;
        }

        #endregion

    }

}