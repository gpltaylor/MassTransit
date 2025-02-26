namespace MassTransit.Context
{
    using System;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using Initializers.TypeConverters;
    using Middleware;
    using Serialization;


    public class MessageSendContext<TMessage> :
        BasePipeContext,
        TransportSendContext<TMessage>
        where TMessage : class
    {
        static readonly TimeSpanTypeConverter _timeSpanConverter = new TimeSpanTypeConverter();

        readonly Lazy<MessageBody> _body;
        readonly DictionarySendHeaders _headers;

        IMessageSerializer _serializer;

        public MessageSendContext(TMessage message, CancellationToken cancellationToken = default)
            : base(cancellationToken)
        {
            Message = message;

            _headers = new DictionarySendHeaders();

            var messageId = NewId.Next();

            MessageId = messageId.ToGuid();
            SentTime = messageId.Timestamp;

            _body = new Lazy<MessageBody>(() => GetMessageBody());
        }

        /// <summary>
        /// Set to true if the message is being published
        /// </summary>
        public bool IsPublish { get; set; }

        public MessageBody Body => _body.Value;

        public virtual TimeSpan? Delay { get; set; }

        public Guid? MessageId { get; set; }
        public Guid? RequestId { get; set; }
        public Guid? CorrelationId { get; set; }

        public Guid? ConversationId { get; set; }
        public Guid? InitiatorId { get; set; }

        public Guid? ScheduledMessageId { get; set; }

        public SendHeaders Headers => _headers;

        public Uri SourceAddress { get; set; }
        public Uri DestinationAddress { get; set; }
        public Uri ResponseAddress { get; set; }
        public Uri FaultAddress { get; set; }

        public TimeSpan? TimeToLive { get; set; }
        public DateTime? SentTime { get; private set; }

        public ContentType ContentType { get; set; }

        public IMessageSerializer Serializer
        {
            get => _serializer;
            set
            {
                if (_body.IsValueCreated)
                    throw new InvalidOperationException("The message was already serialized");

                _serializer = value;
                if (_serializer != null)
                    ContentType = _serializer.ContentType;
            }
        }

        public ISerialization Serialization { get; set; }

        public long? BodyLength => _body.IsValueCreated ? _body.Value.Length : default;

        public SendContext<T> CreateProxy<T>(T message)
            where T : class
        {
            return new SendContextProxy<T>(this, message);
        }

        public bool Durable { get; set; } = true;

        public TMessage Message { get; }

        public bool Mandatory { get; set; }

        public virtual void WritePropertiesTo(IDictionary<string, object> properties)
        {
            if (!Durable)
                properties[PropertyNames.Durable] = false;
            if (Mandatory)
                properties[PropertyNames.Mandatory] = true;
            if (Delay.HasValue)
                properties[PropertyNames.Delay] = Delay.Value;
        }

        public virtual void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
        {
            Durable = ReadBoolean(properties, PropertyNames.Durable, true);
            Mandatory = ReadBoolean(properties, PropertyNames.Mandatory);
            Delay = ReadTimeSpan(properties, PropertyNames.Delay);
        }

        MessageBody GetMessageBody()
        {
            return Serializer?.GetMessageBody(this) ?? throw new SerializationException("Unable to serialize message, no serializer specified.");
        }

        protected static string ReadString(IReadOnlyDictionary<string, object> properties, string key, string defaultValue = null)
        {
            if (properties.TryGetValue(key, out var value))
            {
                if (value is string text)
                    return text;

                if (value is byte[] bytes)
                {
                    text = Encoding.UTF8.GetString(bytes);
                    return text;
                }
            }

            return defaultValue;
        }

        protected static TimeSpan? ReadTimeSpan(IReadOnlyDictionary<string, object> properties, string key, TimeSpan? defaultValue = null)
        {
            var value = ReadString(properties, key);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return _timeSpanConverter.TryConvert(value, out var result) ? result : defaultValue;
        }

        protected static T? ReadEnum<T>(IReadOnlyDictionary<string, object> properties, string key, T? defaultValue = default)
            where T : struct
        {
            if (properties.TryGetValue(key, out var value))
            {
                if (value is string text)
                    return Enum.TryParse<T>(text, out var enumValue) ? enumValue : defaultValue;
            }

            return defaultValue;
        }

        protected static byte ReadByte(IReadOnlyDictionary<string, object> properties, string key, byte defaultValue = default)
        {
            if (properties.TryGetValue(key, out var value))
            {
                if (value is byte byteValue)
                    return byteValue;

                if (value is string text)
                    return byte.TryParse(text, out byteValue) ? byteValue : defaultValue;

                if (value is byte[] bytes)
                {
                    text = Encoding.UTF8.GetString(bytes);
                    return byte.TryParse(text, out byteValue) ? byteValue : defaultValue;
                }
            }

            return defaultValue;
        }

        protected static int? ReadInt(IReadOnlyDictionary<string, object> properties, string key, int? defaultValue = null)
        {
            if (properties.TryGetValue(key, out var value))
            {
                if (value is int intValue)
                    return intValue;

                if (value is string text)
                    return int.TryParse(text, out intValue) ? intValue : defaultValue;

                if (value is byte[] bytes)
                {
                    text = Encoding.UTF8.GetString(bytes);
                    return int.TryParse(text, out intValue) ? intValue : defaultValue;
                }
            }

            return defaultValue;
        }

        protected static bool ReadBoolean(IReadOnlyDictionary<string, object> properties, string key, bool defaultValue = default)
        {
            if (properties.TryGetValue(key, out var value))
            {
                if (value is bool boolValue)
                    return boolValue;

                if (value is string text)
                    return bool.TryParse(text, out boolValue) ? boolValue : defaultValue;

                if (value is byte[] bytes)
                {
                    text = Encoding.UTF8.GetString(bytes);
                    return bool.TryParse(text, out boolValue) ? boolValue : defaultValue;
                }
            }

            return defaultValue;
        }


        static class PropertyNames
        {
            public const string Delay = "Delay";
            public const string Durable = "Durable";
            public const string Mandatory = "Mandatory";
        }
    }
}
