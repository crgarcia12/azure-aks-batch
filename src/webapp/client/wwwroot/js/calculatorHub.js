﻿"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/calculatorHub").build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

connection.on("CalculationStarted", function () {
    document.getElementById("serverMessage").innerHTML = "Calculation has started...";
});
connection.on("ReceiveMessage", function (user, message) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    li.textContent = `${message}`;
});

connection.on("UpdateMessagesReceived", function (messages) {
    document.getElementById("messagesReceived").innerHTML = messages;
})

connection.on("UpdateMessagesSent", function (messages) {
    document.getElementById("messagesSent").innerHTML = messages;
})

connection.on("UpdateMessagesInQueue", function (messages) {
    document.getElementById("messagesInQueue").innerHTML = messages;
})
connection.on("UpdateLastCalculationTimeMs", function (messages) {
    document.getElementById("lastCalculationTimeMs").innerHTML = messages;
})


connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    var sessionId = document.getElementById("sessionId").value;
    var numberOfMessages = document.getElementById("numberOfMessages").value;
    connection.invoke("Calculate", sessionId, numberOfMessages).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});