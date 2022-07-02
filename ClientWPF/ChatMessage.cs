using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientWPF
{
    public enum TypeMessage
    {
        Login,
        Logout,
        Message,
        Photo
    }
    public class ChatMessage
    {
        public TypeMessage MessageType;
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
        public int  ProfileImageSize { get; set; } // розмір фото для аватару
        public byte[] ProfileImage { get; set; } // байти для фото аватару
        public int ImageSize { get; set; } 
        public byte[] Image { get; set; } 
        public byte[] Serialize()
        {
            using(MemoryStream m = new MemoryStream())
            {
                using(BinaryWriter bw = new BinaryWriter(m))
                {
                    bw.Write((int)MessageType);
                    bw.Write(UserId);
                    bw.Write(UserName);
                    if (MessageType == TypeMessage.Photo)
                    {
                        bw.Write(ImageSize);
                        bw.Write(Image);
                    }
                    else {
                        bw.Write(Text);
                        bw.Write(ProfileImageSize);//записання та серіалізація розміру фото для аватару
                        bw.Write(ProfileImage);//записання та серіалізація байтів фото аватару
                    }  
                }
                return m.ToArray();
            }
        }
        public static ChatMessage Deserialize(byte[] data)
        {
            ChatMessage message = new ChatMessage();
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(m))
                {
                    message.MessageType = (TypeMessage)br.ReadInt32();
                    message.UserId = br.ReadString();
                    message.UserName = br.ReadString();
                    if (message.MessageType == TypeMessage.Photo)
                    {
                        message.ImageSize = br.ReadInt32();
                        message.Image = br.ReadBytes(message.ImageSize);
                    }
                    else
                    {
                        message.Text = br.ReadString();
                        message.ProfileImageSize = br.ReadInt32();//десеріалізація розміру фото для аватару
                        message.ProfileImage = br.ReadBytes(message.ProfileImageSize);//десеріалізація байтів фото аватару та перетворення у Image
                    }
                }
            }
            return message;
        }
    }
}
