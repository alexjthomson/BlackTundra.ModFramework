namespace BlackTundra.ModFramework.Model {

    /// <summary>
    /// Stores basic data about a vertex.
    /// </summary>
    internal struct VertexData {

        #region variable

        /// <summary>
        /// Vertex index that the <see cref="VertexData"/> applies to.
        /// </summary>
        public readonly int vertexIndex;

        /// <summary>
        /// UV index that the referenced vertex should use.
        /// </summary>
        public readonly int uvIndex;

        /// <summary>
        /// Normal index that the referenced vertex should use.
        /// </summary>
        public readonly int normalIndex;

        #endregion

        #region constructor

        internal VertexData(in int vertexIndex, in int uvIndex, in int normalIndex) {
            this.vertexIndex = vertexIndex;
            this.uvIndex = uvIndex;
            this.normalIndex = normalIndex;
        }

        #endregion

    }

}