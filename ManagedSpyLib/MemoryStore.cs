using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.ManagedSpy
{
    internal sealed class MemoryStore : IDisposable
    {
        private const int HeaderSize = sizeof(uint);
        private const int MaxTransactionId = 999;

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<int, Dictionary<int, MemoryStore>> GlobalStore =
            new Dictionary<int, Dictionary<int, MemoryStore>>();

        private readonly string paramKey;
        private readonly string retvalKey;
        private readonly int processId;
        private readonly int transactionId;
        private readonly IntPtr notificationWindow;

        private MemoryMappedFile parameterStore;
        private MemoryMappedFile returnValueStore;
        private int referenceCount;
        private bool disposed;

        private MemoryStore(int processId, int transactionId, IntPtr notificationWindow)
        {
            paramKey = "MSFT_ManagedSpy_PARAMS." + processId + "." + transactionId;
            retvalKey = "MSFT_ManagedSpy_RETVAL." + processId + "." + transactionId;
            this.processId = processId;
            this.transactionId = transactionId;
            this.notificationWindow = notificationWindow;
            referenceCount = 1;
        }

        public static MemoryStore CreateStore(IntPtr notificationWindow)
        {
            lock (SyncRoot)
            {
                int currentProcessId = Environment.ProcessId;
                if (!GlobalStore.TryGetValue(currentProcessId, out Dictionary<int, MemoryStore> processStore))
                {
                    processStore = new Dictionary<int, MemoryStore>();
                    GlobalStore.Add(currentProcessId, processStore);
                }

                for (int nextTid = 0; nextTid <= MaxTransactionId; nextTid++)
                {
                    if (!processStore.ContainsKey(nextTid))
                    {
                        MemoryStore store = new MemoryStore(currentProcessId, nextTid, notificationWindow);
                        processStore.Add(nextTid, store);
                        return store;
                    }
                }
            }

            throw new InvalidOperationException("ManagedSpy ran out of cross-process transaction identifiers.");
        }

        public static MemoryStore OpenStore(NativeMethods.CwpStruct message)
        {
            return OpenStore((int)message.wParam, (int)message.lParam, true);
        }

        public static MemoryStore OpenStore(int processId, int transactionId, bool addRef)
        {
            lock (SyncRoot)
            {
                if (!GlobalStore.TryGetValue(processId, out Dictionary<int, MemoryStore> processStore))
                {
                    if (!addRef)
                    {
                        return null;
                    }

                    processStore = new Dictionary<int, MemoryStore>();
                    GlobalStore.Add(processId, processStore);
                }

                if (processStore.TryGetValue(transactionId, out MemoryStore store))
                {
                    if (addRef)
                    {
                        store.AddRef();
                    }

                    return store;
                }

                if (!addRef)
                {
                    return null;
                }

                store = new MemoryStore(processId, transactionId, IntPtr.Zero);
                processStore.Add(transactionId, store);
                return store;
            }
        }

        public bool StoreParameters(object parameters)
        {
            return StoreData(ref parameterStore, parameters, paramKey);
        }

        public bool StoreReturnValue(object returnValue)
        {
            return StoreData(ref returnValueStore, returnValue, retvalKey);
        }

        public object GetParameters()
        {
            return GetData(ref parameterStore, paramKey);
        }

        public object GetReturnValue()
        {
            return GetData(ref returnValueStore, retvalKey);
        }

        public object SendDataMessage(uint message, object parameter)
        {
            if (parameter != null)
            {
                StoreParameters(parameter);
            }

            NativeMethods.SendMessage(notificationWindow, message, (IntPtr)processId, (IntPtr)transactionId);
            return GetReturnValue();
        }

        public int AddRef()
        {
            lock (SyncRoot)
            {
                referenceCount++;
                return referenceCount;
            }
        }

        public int Release()
        {
            bool shouldDispose = false;
            int newReferenceCount;

            lock (SyncRoot)
            {
                if (disposed)
                {
                    return 0;
                }

                referenceCount--;
                newReferenceCount = referenceCount;
                shouldDispose = referenceCount == 0;
            }

            if (shouldDispose)
            {
                Dispose();
            }

            return newReferenceCount;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            lock (SyncRoot)
            {
                if (GlobalStore.TryGetValue(processId, out Dictionary<int, MemoryStore> processStore))
                {
                    processStore.Remove(transactionId);
                }
            }

            parameterStore?.Dispose();
            returnValueStore?.Dispose();
            parameterStore = null;
            returnValueStore = null;

            if (notificationWindow != IntPtr.Zero)
            {
                NativeMethods.SendMessage(
                    notificationWindow,
                    ManagedSpyMessages.ReleaseMemory,
                    (IntPtr)processId,
                    (IntPtr)transactionId);
            }
        }

        private static bool StoreData(ref MemoryMappedFile mapping, object data, string mappingName)
        {
            mapping?.Dispose();
            mapping = null;

            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                formatter.Serialize(stream, data);
            }
            catch (SerializationException)
            {
                stream.Dispose();
                return false;
            }

            byte[] payload = stream.ToArray();
            stream.Dispose();

            mapping = MemoryMappedFile.CreateOrOpen(mappingName, payload.Length + HeaderSize);
            using (MemoryMappedViewAccessor accessor = mapping.CreateViewAccessor(0, payload.Length + HeaderSize))
            {
                accessor.Write(0, (uint)payload.Length);
                accessor.WriteArray(HeaderSize, payload, 0, payload.Length);
            }

            return true;
        }

        private static object GetData(ref MemoryMappedFile mapping, string mappingName)
        {
            if (mapping == null)
            {
                try
                {
                    mapping = MemoryMappedFile.OpenExisting(mappingName);
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
            }

            using (MemoryMappedViewAccessor accessor = mapping.CreateViewAccessor())
            {
                uint size = accessor.ReadUInt32(0);
                byte[] payload = new byte[(int)size];
                accessor.ReadArray(HeaderSize, payload, 0, payload.Length);

                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream stream = new MemoryStream(payload))
                {
                    try
                    {
                        return formatter.Deserialize(stream);
                    }
                    catch (SerializationException)
                    {
                        return null;
                    }
                }
            }
        }
    }
}
