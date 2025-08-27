clean:
	rmdir /Q /S publish
	rm ./BTCPayServer.Plugins.Branta.btcpay

publish:
	dotnet publish -c Release -o ./publish
	Compress-Archive -Path ./publish/* -DestinationPath ./BTCPayServer.Plugins.Branta.zip
	mv ./BTCPayServer.Plugins.Branta.zip ./BTCPayServer.Plugins.Branta.btcpay
