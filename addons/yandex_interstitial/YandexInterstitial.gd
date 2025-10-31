extends Object

signal onInterstitialLoaded
signal onInterstitialError(error: String)
signal onInterstitialClosed
signal onDebugMessage(msg: String)

func loadInterstitial(block_id: String):
	pass

func showInterstitial():
	pass