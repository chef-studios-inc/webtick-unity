class WebGLContext {
	constructor() {
		this.gameObject = ""
		this.req_id = 1;
		this.promResolvers = new Map();
		this.sendMessageProm = new Promise((res, rej) => {
			this.sendMessageRes = res;
		});
		this.setGameObjectProm = new Promise((res, rej) => {
			this.setGameObjectRes = res;
		});
		this.sendMessage = function () { }
	}

	setSendMessage(sendMessageFn) {
		this.sendMessage = sendMessageFn;
		this.sendMessageRes();
	}

	setGameObject(gameObject) {
		if(this.gameObject !== "") {
			console.error("Only one WebGLContext supported");
			return;
		}
		this.gameObject = gameObject;
		this.setGameObjectRes();
	}

	async getCtx() {
		await this.initialized();
		const req = this.req_id;
		this.req_id++;
		const prom = new Promise((res, rej) => {
			this.promResolvers[req] = res;
			this.sendMessage(this.gameObject, "GetCtx", req)
		});
		return prom;
	}

	getCtxResponse(req_id, ctx) {
		const resolver = this.promResolvers[req_id];
		if(!resolver) {
			console.error("Couldn't find resolver for getCtxResponse request: ", req_id);
			return;
		}

		resolver(ctx);
		this.promResolvers.delete(req_id);
	}

	async getTexture() {
		await this.initialized();
		const req_id = this.req_id;
		this.req_id++;
		const prom = new Promise((res, rej) => {
			this.promResolvers[req_id] = res;
			this.sendMessage(this.gameObject, "GetTexture", req_id)
		});
		return prom;
	}

	getTextureResponse(req_id, tex) {
		const resolver = this.promResolvers[req_id];
		if(!resolver) {
			console.error("Couldn't find resolver for getTextureResponse request: ", req_id);
			return;
		}

		resolver(tex);
		this.promResolvers.delete(req_id);
	}

	async destroyTexture(tex) {
		await this.initialized();
		const req_id = this.req_id;
		this.req_id++;
		const prom = new Promise((res, rej) => {
			this.promResolvers[req] = res;
			this.sendMessage(this.gameObject, "DestroyTexture", req_id, tex)
		});
		return prom;
	}

	destroyTextureResponse(req_id) {
		const resolver = this.promResolvers[req_id];
		if(!resolver) {
			console.error("Couldn't find resolver for destroyTextureResponse request: ", req_id);
			return;
		}

		resolver();
		this.promResolvers.delete(req_id);
	}

	async initialized() {
		await this.sendMessageProm;
		await this.setGameObjectProm;
	}
}

window.webtick = Object.assign(window.webtick || {}, { webGLContext: new WebGLContext});