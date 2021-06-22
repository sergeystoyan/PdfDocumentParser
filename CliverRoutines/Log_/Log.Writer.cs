//********************************************************************************************
//Author: Sergey Stoyan
//        sergey.stoyan@gmail.com
//        sergey.stoyan@hotmail.com
//        stoyan@cliversoft.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Cliver
{
    public partial class Log
    {
        /// <summary>
        /// The base session-less log writer that is enherited by the NamedWriter and ThreadWriter. 
        /// Also, by itself, it allows to write a session-less named log directly to WorkDir. 
        /// </summary>
        public partial class Writer
        {
            static public Writer Get(string name)
            {
                lock (names2Writer)
                {
                    if (!names2Writer.TryGetValue(name, out Writer w))
                    {
                        w = new Writer(name);
                        w.SetFile();
                        names2Writer.Add(name, w);
                    }
                    return w;
                }
            }
            static Dictionary<string, Writer> names2Writer = new Dictionary<string, Writer>();

            internal Writer(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Message importance level.
            /// </summary>
            virtual public Level Level
            {
                get
                {
                    return level;
                }
                set
                {
                    lock (this)
                    {
                        if (level == Level.NONE && value > Level.NONE)
                            setWorkDir(true);
                        level = value;
                    }
                }
            }
            protected Level level = Log.DefaultLevel;

            /// <summary>
            /// Log name.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Log file path.
            /// </summary>
            public string File { get; protected set; } = null;

            virtual internal void SetFile()
            {
                lock (this)
                {
                    //(!)it must differ from the session files to avoid overlapping
                    string file2 = WorkDir + System.IO.Path.DirectorySeparatorChar + "__" + Name + (fileCounter > 0 ? "[" + fileCounter + "]" : "") + "." + FileExtension;

                    if (File == file2)
                        return;
                    if (logWriter != null)
                        logWriter.Close();
                    File = file2;
                }
            }
            protected int fileCounter = 0;

            /// <summary>
            /// Maximum log file length in bytes.
            /// If negative than no effect.
            /// </summary>
            public int MaxFileSize = Log.DefaultMaxFileSize;

            /// <summary>
            /// Close the log
            /// </summary>
            public void Close()
            {
                lock (this)
                {
                    if (logWriter != null)
                        logWriter.Close();
                    logWriter = null;
                }
            }

            internal bool IsClosed
            {
                get
                {
                    return logWriter == null;
                }
            }

            /// <summary>
            /// Base writting log method.
            /// </summary>
            public void Write(Log.MessageType messageType, string message, string details = null)
            {
                lock (this)
                {
                    write(messageType, message, details);
                    if (messageType == Log.MessageType.EXIT)
                        Environment.Exit(0);
                }
            }
            void write(Log.MessageType messageType, string message, string details = null)
            {
                lock (this)
                {
                    Writing?.Invoke(Name, messageType, message, details);

                    switch (Level)
                    {
                        case Level.NONE:
                            return;
                        case Level.ERROR:
                            if (messageType < MessageType.ERROR)
                                return;
                            break;
                        case Level.WARNING:
                            if (messageType < MessageType.WARNING)
                                return;
                            break;
                        case Level.INFORM:
                            if (messageType < MessageType.INFORM)
                                return;
                            break;
                        case Level.ALL:
                            break;
                        default:
                            throw new Exception("Unknown option: " + Level);
                    }

                    if (MaxFileSize > 0)
                    {
                        FileInfo fi = new FileInfo(File);
                        if (fi.Exists && fi.Length > MaxFileSize)
                        {
                            fileCounter++;
                            SetFile();
                        }
                    }

                    if (logWriter == null)
                    {
                        Directory.CreateDirectory(PathRoutines.GetFileDir(File));
                        logWriter = new StreamWriter(File, true);
                    }

                    message = (messageType == MessageType.LOG ? "" : messageType.ToString() + ": ") + message + (string.IsNullOrWhiteSpace(details) ? "" : "\r\n\r\n" + details);
                    logWriter.WriteLine(DateTime.Now.ToString(Log.TimePattern) + message);
                    logWriter.Flush();
                }
            }
            protected TextWriter logWriter = null;

            /// <summary>
            /// Called for Writing. 
            /// </summary>
            /// <param name="logWriterName"></param>
            /// <param name="messageType"></param>
            /// <param name="message"></param>
            /// <param name="details"></param>
            public delegate void OnWrite(string logWriterName, Log.MessageType messageType, string message, string details);
            /// <summary>
            /// Triggered before writing message.
            /// </summary>
            static public event OnWrite Writing = null;
        }
    }
}