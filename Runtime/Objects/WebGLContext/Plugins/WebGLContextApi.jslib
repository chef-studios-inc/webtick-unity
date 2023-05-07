mergeInto(LibraryManager.library, {
	WebGLContext_Initialize: function(gameObject) {
    window.webtick.webGLContext.setSendMessage(SendMessage);
		window.webtick.webGLContext.setGameObject(UTF8ToString(gameObject));
	},

  WebGLContext_GetTextureResponse: function (req_id) {
    var tex = GLctx.createTexture();
    if (!tex) {
      console.error("Failed to create a new texture");
      return;
    }
    var id = GL.getNewId(GL.textures);
    tex.name = id;
    GL.textures[id] = tex;
		window.webtick.webGLContext.getTextureResponse(req_id, tex);
  },

  WebGLContext_DestroyTextureResponse: function (req_id, native_texture_id) {
    GLctx.deleteTexture(GL.textures[native_texture_id]);
		window.webtick.webGLContext.destroyTextureResponse(req_id);
  },

  WebGLContext_GetCtxResponse: function (req_id) {
		window.webtick.webGLContext.getCtxResponse(req_id, GLctx);
  },

});