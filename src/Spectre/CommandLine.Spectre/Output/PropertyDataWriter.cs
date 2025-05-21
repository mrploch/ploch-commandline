// using System.Management;
//
// namespace Ploch.Tools.SystemsProfiles.UI.ConsoleUI.Common.Output;
//
// public class PropertyDataWriter(IMessageFormatterProcessor formatterProcessor) : TypeMessageWriter<PropertyData>
// {
//     
//     public override Type MessageType => typeof(PropertyData);
//
//     public override bool CanHandle(object? message) => message is PropertyData;
//
//     public override void Write(PropertyData? message, IMessageFormatterProcessor? formatterProcessor = null)
//     {
//         throw new NotImplementedException();
//     }
//
//     protected override string GetMessageText(PropertyData? message, string? markupTag = null)
//     {
//         if (message == null)
//         {
//             return string.Empty;
//         }
//
//         return $"{message.Name}: {message.Value}";
//     }
// }
//
// {
// // public override void Write(PropertyData? message, IMessageFormatterProcessor? formatterProcessor = null)
// // {
// //     throw new NotImplementedException();
// // }
// // }


