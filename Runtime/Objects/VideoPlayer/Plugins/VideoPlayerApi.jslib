mergeInto(LibraryManager.library, {
	VideoPlayer_Create: async function(gameObject, url) {
		const go = UTF8ToString(gameObject);
		const texture = await window.webtick.videoPlayerApi.createNewVideoPlayer(go, UTF8ToString(url));
		SendMessage(go, "OnVideoCreated", texture);
	},

	VideoPlayer_Destroy: function(gameObject) {
		return window.webtick.videoPlayerApi.destroyVideoPlayer(UTF8ToString(gameObject));
	},

	VideoPlayer_Play: function(gameObject) {
		return window.webtick.videoPlayerApi.play(UTF8ToString(gameObject));
	},

	VideoPlayer_SetVolume: function(gameObject, volume) {
		return window.webtick.videoPlayerApi.setVolume(UTF8ToString(gameObject), volume);
	},

	VideoPlayer_SetFPS: function(gameObject, FPS) {
		return window.webtick.videoPlayerApi.setFPS(UTF8ToString(gameObject), FPS);
	}
	
});