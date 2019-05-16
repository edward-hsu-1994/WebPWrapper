using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// WebP編碼器
    /// </summary>
    public class WebPEncoder : IWebPEncoder {
        private string _executeFilePath;
        private string _arguments;
        internal WebPEncoder(string executeFilePath, string arguments) {
            _executeFilePath = executeFilePath;
            _arguments = arguments;
        }

        public void Encode(Stream input, Stream output) {
            var inputFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();

            input.Seek(0, SeekOrigin.Begin);

            using (var inputStream = File.Open(inputFile, FileMode.Open)) {
                input.CopyTo(inputStream);
            }

            try {
                using (Process webpProcess = new Process()) {
                    var stdout = "";
                    try {
                        webpProcess.StartInfo.UseShellExecute = false;
                        webpProcess.StartInfo.FileName = _executeFilePath;
                        webpProcess.StartInfo.Arguments = _arguments + $" -o {outputFile} {inputFile}";
                        webpProcess.StartInfo.CreateNoWindow = true;
                        webpProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                            stdout += e.Data;
                        };
                        webpProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                            stdout += e.Data;
                        };
                        webpProcess.Start();
                        webpProcess.WaitForExit();
                    } catch (Exception e) {
                        File.Delete(inputFile);
                        File.Delete(outputFile);

                        throw new Exception(stdout);
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            using (var outputStream = File.Open(outputFile, FileMode.Open)) {
                outputStream.CopyTo(output);
            }

            File.Delete(inputFile);
            File.Delete(outputFile);
        }


        public async Task EncodeAsync(Stream input, Stream output) {
            await Task.Run(() => Encode(input, output));
        }
    }
}
