const livekit = {
	$LiveKitData: {
		room: null,
		buffer: [],
		serverParticipant: null,
	},

	LK_ConnectToRoom: function(urlStr, tokenStr) {
		var token = UTF8ToString(tokenStr);
		var url = UTF8ToString(urlStr);
		console.log("Connecting to room", url, token)

		LiveKitData.room = new window.livekit.Room();

		LiveKitData.room.on('disconnected', () => {
			LiveKitData.room = null;
			LiveKitData.serverParticipant = null;
			LiveKitData.buffer = []
		});

		LiveKitData.room.on('participantConnected', (p) => {
			if(p.identity === "server") {
				LiveKitData.serverParticipant = p;
			}
		});

		LiveKitData.room.on('participantDisconnected', (p) => {
			if(p.identity === "server") {
				LiveKitData.serverParticipant = null;
			}
		});

		LiveKitData.room.connect(url, token).then(() => {
			LiveKitData.room.participants.forEach((value, key) => {
				if (value.identity === "server") {
					LiveKitData.serverParticipant = value;
				}
			})
		}).catch(function (err) {
			console.error("Error connecting to livekit", err);
		});

		LiveKitData.room.on('dataReceived', (payload, sender) => {
			if(sender.identity !== "server") {
				return;
			}
			LiveKitData.buffer.push(payload);
		});
	},

	LK_ReceiveData: function (data, size) {
		if(!LiveKitData.serverParticipant) {
			return -1;
		}
		if(LiveKitData.buffer.length == 0) {
			return 0;
		}
		const buffer = LiveKitData.buffer.shift();
		if (buffer.length > size)
			return 0;
		HEAP8.set(buffer, data);
		return buffer.length;
	},

	LK_SendMessageToServer: function(payload, payload_size) {
		if(!LiveKitData.serverParticipant) {
			console.error("Trying to send message with no server participant");
			return;
		}
		if(!LiveKitData.room || LiveKitData.room.state !== 'connected') {
			console.error("No connected room when publishing data", LiveKitData.room);
			return;
		}

		var payloadArr = new Uint8Array(HEAPU8.buffer, payload, payload_size);
		LiveKitData.room.localParticipant.publishData(payloadArr, 1, [LiveKitData.serverParticipant]);
	},

	LK_IsConnected: function() {
		return LiveKitData.serverParticipant != null;
	},

	LK_Cleanup: function() {
		LiveKitData.serverParticipant = null;
		LiveKitData.livekitGameObject = null;
	}
};
autoAddDeps(livekit, '$LiveKitData');
mergeInto(LibraryManager.library, livekit);