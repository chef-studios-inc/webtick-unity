class VideoPlayer {
	constructor(url, texture, ctx) {
		this.texture = texture;
		this.videoEl = document.createElement("video");
		this.videoEl.style.display = 'hidden';
		//document.body.appendChild(this.videoEl);
		this.videoEl.crossOrigin = "anonymous"
		this.videoEl.loop = true;
		this.videoEl.src = url;
		this.lastTick = Date.now();
		this.ctx = ctx;
		this.ticking = false;
		this.tick = this.tick.bind(this);
		this.FPS = 24;
	}

	play() {
		this.videoEl.play();
		this.videoEl.muted = true;
		this.ticking = true;
		this.tick();
	}

	pause() {
		this.videoEl.pause();
		this.ticking = false;
	}

	destroy() {
		this.videoEl.pause();
		this.videoEl.url = null;
		this.videoEl.remove();
		this.videoEl = null;
	}

	tick() {
		if(!this.ticking) {
			return;
		}

		const currentTime = Date.now();
		// limit to 30fps
		if(currentTime - this.lastTick < (1000 / this.FPS)) {
			requestAnimationFrame(this.tick);
			return;
		}

		this.lastTick = currentTime;

		this.ctx.bindTexture(this.ctx.TEXTURE_2D, this.texture);
    this.ctx.pixelStorei(this.ctx.UNPACK_FLIP_Y_WEBGL, true);
    this.ctx.texImage2D(
      this.ctx.TEXTURE_2D,
      0,
      this.ctx.RGBA,
      this.ctx.RGBA,
      this.ctx.UNSIGNED_BYTE,
			this.videoEl
    );
    this.ctx.pixelStorei(this.ctx.UNPACK_FLIP_Y_WEBGL, false);
    this.ctx.texParameteri(
      this.ctx.TEXTURE_2D,
      this.ctx.TEXTURE_MAG_FILTER,
      this.ctx.LINEAR
    );
    this.ctx.texParameteri(
      this.ctx.TEXTURE_2D,
      this.ctx.TEXTURE_MIN_FILTER,
      this.ctx.LINEAR
    );
    this.ctx.texParameteri(
      this.ctx.TEXTURE_2D,
      this.ctx.TEXTURE_WRAP_S,
      this.ctx.CLAMP_TO_EDGE
    );
    this.ctx.texParameteri(
      this.ctx.TEXTURE_2D,
      this.ctx.TEXTURE_WRAP_T,
      this.ctx.CLAMP_TO_EDGE
    );
    this.ctx.clearColor(0, 0, 0, 0);

		requestAnimationFrame(this.tick);
	}

	setVolume(volume) {
		this.videoEl.muted = volume === 0;
		this.videoEl.volume = volume;
	}

	setFPS(fps) {
		this.FPS = fps;
	}

}

class VideoPlayerApi {
	constructor() {
		this.videoPlayers = new Map();
	}

	async createNewVideoPlayer(gameObject, url) {
		const texture = await window.webtick.webGLContext.getTexture();
		const ctx = await window.webtick.webGLContext.getCtx();
		this.videoPlayers.set(gameObject, new VideoPlayer(url, texture, ctx));
		return texture.name;
	}

	async destroyVideoPlayer(gameObject) {
		if(!this.videoPlayers.has(gameObject)) {
			console.error("No video player for game object", gameObject);
			return;
		}
		const vp = this.videoPlayers.get(gameObject);
		if(vp.texture) {
			await window.webtick.webGLContext.destroyTexture(vp.texture.name);
		}

		this.videoPlayers.get(gameObject).destroy();
		this.videoPlayers.delete(gameObject);
	}

	setVideoPlayerPositionAndForward(handle, pos, forward) {
		if(!this.videoPlayers.has(handle)) {
			console.error("No video player for handle");
			return;
		}
	}

	play(gameObject) {
		if(!this.videoPlayers.has(gameObject)) {
			console.error("No video player for game object when trying to play", gameObject, this.videoPlayers);
			return;
		}
		const vp = this.videoPlayers.get(gameObject);
		vp.play();
	}

	setVolume(gameObject, volume) {
		if(!this.videoPlayers.has(gameObject)) {
			console.error("No video player for game object when trying to setVolume", gameObject, this.videoPlayers);
			return;
		}
		const vp = this.videoPlayers.get(gameObject);
		vp.setVolume(volume);
	}

	setFPS(gameObject, FPS) {
		if(!this.videoPlayers.has(gameObject)) {
			console.error("No video player for game object when trying to setFPS", gameObject, this.videoPlayers);
			return;
		}
		const vp = this.videoPlayers.get(gameObject);
		vp.setFPS(FPS);
	}
}

window.webtick = Object.assign(window.webtick || {}, { videoPlayerApi: new VideoPlayerApi() });