mergeInto(LibraryManager.library, {
	LK_InitializeBridge: function(gameObjectStr) {
		window.webtick.livekitGameObject = UTF8ToString(gameObjectStr);
	},

	LK_ConnectToRoom: function(urlStr, tokenStr) {
		var token = UTF8ToString(tokenStr);
		var url = UTF8ToString(urlStr);

		window.webtick.room = new window.livekit.Room();

		window.webtick.room.on('disconnected', function(){
			console.log("LiveKitRoom Disconnected");
				SendMessage(window.webtick.livekitGameObject, "Web_DisconnectedCallback");
		});

		window.webtick.room.on('participantConnected', function(p) {
			if(p.identity === "server") {
				window.webtick.serverParticipant = p;
				SendMessage(window.webtick.livekitGameObject, "Web_ConnectedCallback");
			}
		});

		window.webtick.room.on('participantDisconnected', function(p) {
			if(p.identity === "server") {
				window.webtick.serverParticipant = null;
			}
		});

		window.webtick.room.connect(url, token).then(() => {
			window.webtick.room.participants.forEach(function (value, key) {
				if (value.identity === "server") {
					window.webtick.serverParticipant = value;
				}
			})

			if (window.webtick.serverParticipant) {
				SendMessage(window.webtick.livekitGameObject, "Web_ConnectedCallback");
			}
		}).catch(function (err) {
			console.error("Error connecting to livekit", err);
		});

		window.webtick.room.on('dataReceived', function(payload, sender) {
			if(sender.identity !== "server") {
				return;
			}
			SendMessage(window.webtick.livekitGameObject, "Web_DataReceived", payload);
		});
	},

	LK_SendMessageToServer: function(payload, payload_size) {
		if(!window.webtick.serverParticipant) {
			console.error("Trying to send message with no server participant");
			return;
		}
		if(!window.webtick.room || window.webtick.room.state !== 'connected') {
			console.error("No connected room when publishing data", window.webtick.room);
			return;
		}

		var payloadArr = new Uint8Array(HEAPU8.buffer, payload, payload_size);
		console.log("NEIL sending payload", payload, payload_size);
		window.webtick.room.localParticipant.publishData(payloadArr, 1, [window.webtick.serverParticipant]);
	},

	LK_Cleanup: function() {
		window.webtick.serverParticipant = null;
		window.webtick.livekitGameObject = null;
	}
});