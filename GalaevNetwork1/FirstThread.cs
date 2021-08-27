using System.Collections;
using System.Text;
using System.Threading;
using GalaevNetwork1.BitArrayRoutine;

namespace GalaevNetwork1
{
    public class FirstThread
    {
        private Semaphore _sendSemaphore;
        private Semaphore _receiveSemaphore;
        private BitArray _sendMessage;
        private PostToSecondWT _post;

        public FirstThread(ref Semaphore sendSemaphore, ref Semaphore receiveSemaphore)
        {
            _sendSemaphore = sendSemaphore;
            _receiveSemaphore = receiveSemaphore;
        }

        public void FirstThreadMain(object obj)
        {
            _post = (PostToSecondWT)obj;

            // + Подчинение - 1 поток устанавливает и управляет соединением
            // + Симплекс - передача только в одном направлении
            // + Покадровая синхронизация - отправляем кадр, но из-за симплекса, не ждём ответа.
            // + Переменная длина - количество данных в одном кадре может изменяться
            // + Указание длины кадра в заголовке - в поле Control записываем длину
            // + Квитирование: простой - дожидаемся получения кадра принимающей стороной (уже реализовано)
            // + Контроль ошибок контрольной суммой - CRC16
            // + Таймаут на получение - в принимающем потоке таймаут на семафор
            // + Буферизация на N кадров - получатель сохраняет N полученных кадров и потом обрабатывает их все разом
            // + Передача последовательности кадров - на вход имеем массив кадров, передаём его целиком.

            string dataString = "Алекс Городников ёбаный негр";
            var dataBytes = Encoding.UTF8.GetBytes(dataString);
            var dataBitArray = new BitArray(dataBytes);

            var inputBitArrays = dataBitArray.Split(C.MaxFrameDataSize);

            ConsoleHelper.WriteToConsole("1 поток", "Начинаю работу.");

            ConsoleHelper.WriteToConsole("1 поток", "Начало передачи");
            _sendMessage = BuildStartFrame().Build();
            _post(_sendMessage);
            _sendSemaphore.Release();
            ConsoleHelper.WriteToConsole("1 поток", "Подключен");
            _receiveSemaphore.WaitOne();

            for (int i = 0; i < inputBitArrays.Count; i++)
            {
                _sendMessage = BuildDataFrame(inputBitArrays[i], i).Build();
                _post(_sendMessage);
                _sendSemaphore.Release();
                ConsoleHelper.WriteToConsole("1 поток", $"Передан кадр {i}");
                _receiveSemaphore.WaitOne();
            }

            ConsoleHelper.WriteToConsole("1 поток", "Конец передачи");
            _sendMessage = BuildEndFrame().Build();
            _post(_sendMessage);
            _sendSemaphore.Release();
            ConsoleHelper.WriteToConsole("1 поток", "Отключен");
            _receiveSemaphore.WaitOne();

            ConsoleHelper.WriteToConsole("1 поток", "Завершаю работу.");
        }

        private Frame BuildStartFrame()
        {
            var frame = new Frame(new BitArray(C.ControlSize), new BitArray(0));

            var bitArrayWriter = new BitArrayWriter(frame.Control);
            bitArrayWriter.Write(new BitArray(new[] {(byte)15, (byte)0, (byte)0}));

            return frame;
        }

        private static Frame BuildDataFrame(BitArray data, int i)
        {
            var frame = new Frame(new BitArray(C.ControlSize), data);

            var bitArrayWriter = new BitArrayWriter(frame.Control);
            bitArrayWriter.Write(new BitArray(new[] {(byte)7, (byte)0, (byte)i}));

            return frame;
        }

        private static Frame BuildEndFrame()
        {
            var frame = new Frame(new BitArray(C.ControlSize), new BitArray(0));

            var bitArrayWriter = new BitArrayWriter(frame.Control);
            bitArrayWriter.Write(new BitArray(new[] {(byte)31, (byte)0, (byte)0}));

            return frame;
        }

        public void ReceiveData(BitArray array)
        {
        }
    }
}