var lsp = require("vscode-languageserver");
var connection = lsp.createConnection();
var documents = new lsp.TextDocuments();

documents.onDidOpen(_ => {
    connection.sendNotification('open', _);
});
documents.onDidChangeContent(_ => {
    connection.sendNotification('changed', _);
});
documents.listen(connection);

connection.onInitialize(_ => {
    return {
        capabilities: {
            textDocumentSync: lsp.TextDocumentSyncKind.Full
        },
        result: null
    };
});
connection.listen();