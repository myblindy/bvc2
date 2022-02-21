using bvc2.LexerCode;

using var inputStream = new MemoryStream();

using (var writer = new StreamWriter(inputStream, Encoding.UTF8, leaveOpen: true))
    writer.Write(@"
var a = 10;
");

inputStream.Position = 0;
var lexer = new Lexer(inputStream);