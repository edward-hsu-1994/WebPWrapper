using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebPWrapper.Decoder {
    /// <summary>
    /// Default WebP decoder
    /// </summary>
    public class WebPDecoder : IWebPDecoder {
        private string _executeFilePath;
        private string _arguments;
        internal WebPDecoder(string executeFilePath, string arguments) {
            _executeFilePath = executeFilePath;
            _arguments = arguments;
        }
        
        public void Decode(Stream input, Stream output) {
            var inputFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();

            try {
                using (var inputStream = File.Open(inputFile, FileMode.Open)) {
                    input.CopyTo(inputStream);
                }

                using (Process webpProcess = new Process()) {
                    var stdout = "";
                    try {
                        webpProcess.StartInfo.UseShellExecute = false;
                        webpProcess.StartInfo.FileName = _executeFilePath;
                        webpProcess.StartInfo.Arguments = _arguments + $" -o {outputFile} {inputFile}";
                        webpProcess.StartInfo.CreateNoWindow = true;
                        webpProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                            stdout += e.Data + "\r\n";
                        };
                        webpProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                            stdout += e.Data + "\r\n";
                        };
                        webpProcess.StartInfo.RedirectStandardError = true;
                        webpProcess.StartInfo.RedirectStandardOutput = true;
                        webpProcess.Start();
                        webpProcess.BeginOutputReadLine();
                        webpProcess.BeginErrorReadLine();
                        webpProcess.WaitForExit();

                        if (webpProcess.ExitCode != 0) {
                            throw new Exception(stdout);
                        }
                    } catch (Exception e) {
                        File.Delete(inputFile);
                        File.Delete(outputFile);

                        if (stdout == "") {
                            throw e;
                        } else {
                            throw new Exception(stdout);
                        }
                    }
                }

                using (var outputStream = File.Open(outputFile, FileMode.Open)) {
                    outputStream.CopyTo(output);
                }
            } finally {
                File.Delete(inputFile);
                File.Delete(outputFile);
            }
        }
        
        public async Task DecodeAsync(Stream input, Stream output) {
            await Task.Run(() => Decode(input, output));
        }
    }
}
