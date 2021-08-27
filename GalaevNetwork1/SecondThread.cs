using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GalaevNetwork1.BitArrayRoutine;

namespace GalaevNetwork1
{
    public class SecondThread
    {
        private Semaphore _sendSemaphore;
        private Semaphore _receiveSemaphore;
        private BitArray _receivedMessage;
        private BitArray _sendMessage;
        private PostToFirstWT _post;

        private Frame[] _receiveBuffer = new Frame[C.ReceiverBufferSize];
        private int _receiveBufferCount = 0;
        private List<BitArray> _receivedDataBits = new();

        public SecondThread(ref Semaphore sendSemaphore, ref Semaphore receiveSemaphore)
        {
            _sendSemaphore = sendSemaphore;
            _receiveSemaphore = receiveSemaphore;
        }

        public void SecondThreadMain(Object obj)
        {
            _post = (PostToFirstWT)obj;
            ConsoleHelper.WriteToConsole("2 поток", "Начинаю работу.");

            WaitForDataWithTimeout();

            var startFrame = Frame.Parse(_receivedMessage);
            if (startFrame.Control.ToByteArray()[0] != 15)
            {
                // Error, start frame with invalid control
            }

            _sendSemaphore.Release();

            while (true)
            {
                WaitForDataWithTimeout();
                var frame = Frame.Parse(_receivedMessage);

                if (frame.Control.ToByteArray()[0] == 31)
                {
                    // Received end frame

                    DrainBuffer();

                    var receivedString = GetReceivedString();

                    ConsoleHelper.WriteToConsole("2 поток", $"Получены данные: \"{receivedString}\"");

                    _sendSemaphore.Release();
                    break;
                }
                else if (frame.Control.ToByteArray()[0] == 7)
                {
                    var receivedChecksum = frame.Checksum;
                    var actualChecksum = frame.BuildChecksum();
                    if (!receivedChecksum.IsSameNoCopy(actualChecksum, 0, 0, C.ChecksumSize))
                    {
                        ConsoleHelper.WriteToConsole("2 поток", "Контрольная сумма кадра не совпала!");
                    }
                    else
                    {
                        _receiveBuffer[_receiveBufferCount++] = frame;
                        if (_receiveBufferCount == C.ReceiverBufferSize)
                        {
                            DrainBuffer();
                        }
                    }

                    _sendSemaphore.Release();
                }
            }

            ConsoleHelper.WriteToConsole("2 поток", "Заканчиваю работу");
        }

        private string GetReceivedString()
        {
            var totalReceivedBitsCount = _receivedDataBits.Sum(b => b.Count);
            var totalReceivedBits = new BitArray(totalReceivedBitsCount);
            var totalReceivedBitsWriter = new BitArrayWriter(totalReceivedBits);
            for (int i = 0; i < _receivedDataBits.Count; i++)
            {
                totalReceivedBitsWriter.Write(_receivedDataBits[i]);
            }

            var receivedBytes = totalReceivedBits.ToByteArray();

            var receivedString = Encoding.UTF8.GetString(receivedBytes);
            return receivedString;
        }

        private void DrainBuffer()
        {
            for (int i = 0; i < _receiveBufferCount; i++)
            {
                _receivedDataBits.Add(_receiveBuffer[i].Data);
            }

            _receiveBufferCount = 0;
        }

        private void WaitForDataWithTimeout()
        {
            int tries = 0;
            while (!_receiveSemaphore.WaitOne(500) && tries < 3)
            {
                // Timeout
                ConsoleHelper.WriteToConsole("2 поток", $"Таймаут получения {++tries} раз");
            }
        }

        public void ReceiveData(BitArray array)
        {
            _receivedMessage = array;
        }
    }
}