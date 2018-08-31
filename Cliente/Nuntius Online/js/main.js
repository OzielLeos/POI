
(function () {
    //"use strict";

    var app = angular.module('NuntiusApp', []);
    app.controller('NuntiusCtrl', function ($scope) {
        $scope.user = "";
        $scope.recivido = "fff";
        $scope.LogMe = function () {
            LogUser($scope.user);

        }

  
        var reader;
window.onload = function () {

    openClient();

    

    reader = new Windows.Storage.Streams.DataReader(socketsSample.clientSocket.inputStream);
    
}
function openClient() {
    if (socketsSample.clientSocket) {
        //  WinJS.log("Already have a client; call close to close the listener and the client.", "", "error");
        return;
    }

    var serviceName = "9050";
    if (serviceName === "") {
        //  WinJS.log("Please provide a service name.", "", "error");
        return;
    }

    // By default 'hostNameConnect' is disabled and host name validation is not required. When enabling the text
    // box validating the host name is required since it was received from an untrusted source (user input).
    // The host name is validated by catching exceptions thrown by the HostName constructor.
    // Note that when enabling the text box users may provide names for hosts on the Internet that require the
    // "Internet (Client)" capability.
    var hostName;
    try {
        hostName = new Windows.Networking.HostName("127.0.0.1");
    } catch (error) {
        // WinJS.log("Error: Invalid host name.", "", "error");
        return;
    }

    socketsSample.closing = false;
    socketsSample.clientSocket = new Windows.Networking.Sockets.StreamSocket();

    if (socketsSample.adapter === null) {
        //  WinJS.log("Connecting to: " + hostNameConnect.textContent, "", "status");
        socketsSample.clientSocket.connectAsync(hostName, serviceName).done(function () {
            //  WinJS.log("Connected", "", "status");
            socketsSample.connected = true;
        }, onError);
    } else {
        //  WinJS.log(
        //    "Connecting to: " + hostNameConnect.textContent + " using network adapter " + socketsSample.adapter.networkAdapterId,
        //     "",
        //    "status");

        // Connect to the server (in our case the listener we created in previous step)
        // limiting traffic to the same adapter that the user specified in the previous step.
        // This option will be overridden by interfaces with weak-host or forwarding modes enabled.
        socketsSample.clientSocket.connectAsync(
            hostName,
            serviceName,
            Windows.Networking.Sockets.SocketProtectionLevel.plainSocket,
            socketsSample.adapter).done(function () {
                // WinJS.log("Connected using network adapter " + socketsSample.adapter.networkAdapterId, "", "status");
                socketsSample.connected = true;
            }, onError);
    }
}
function onError(reason) {
    socketsSample.clientSocket = null;

    // When we close a socket, outstanding async operations will be canceled and the
    // error callbacks called.  There's no point in displaying those errors.
    if (!socketsSample.closing) {
        // WinJS.log(reason, "", "error");
    }
}
function LogUser(string) {

    userName = "";
    var userState = "";
    userName = string;

    userState = "Connected";

    var userData = userName + "\r\n" + userState;
    sendstring(userData);
    ////add to array
    //document.getElementById("DivLog").style.visibility = "hidden";
    //document.getElementById("UsersList").style.visibility = "visible";
    ////recibir datos

    recvstring();
}

function sendstring(string) {
    if (!socketsSample.connected) {
        return;
    }
    var writer = new Windows.Storage.Streams.DataWriter(socketsSample.clientSocket.outputStream);
    // string = document.getElementById("myText").value;
    var len = writer.measureString(string); // Gets the UTF-8 string length.
    writer.writeInt32(len);
    writer.writeString(string);
    writer.storeAsync().done(function () {
        writer.detachStream();
    }, onError);
}

function recvstring() {
    if (!socketsSample.connected) {
        return;
    }
   
    
     var receivedData;
      
    var sizeBytes;

    reader.loadAsync(4).done(function (sizeBytesRead) {
        sizeBytes = sizeBytesRead;
    
    });
    var bytes = new Uint8Array(sizeBytes);
    reader.readBytes(bytes);
    var count = 0;
    for (var i = bytes.length - 1; i >= 0; i--) {
        count = (count * 256) + bytes[i];
    }

    reader.loadAsync(count).done(function (stringBytesRead) {
        sizeBytes = stringBytesRead;

    });

        receivedData = reader.readString(sizeBytes);
    $scope.recivido = receivedData;
    $scope.recivido = $scope.recivido ;


}


    });
})();