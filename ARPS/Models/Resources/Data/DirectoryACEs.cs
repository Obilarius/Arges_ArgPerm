﻿namespace ARPS
{
    public class DirectoryACEs
    {
        public bool IsGroup { get; }
        public string IdentityName { get; }
        public string DistinguishedName { get; }
        public int ACEId { get; }
        public string SID { get; }
        public int Rights { get; }
        public bool Type { get; }
        public string FileSystemRight { get; }
        public bool IsInherited { get; }
        public int InheritanceFlags { get; }
        public int PropagationFlags { get; }


        public DirectoryACEs(bool isGroup, string identityName, string distinguishedName, int aCEId, string sID, int rights, bool type, string fileSystemRight, bool isInherited, int inheritanceFlags, int propagationFlags)
        {
            IsGroup = isGroup;
            IdentityName = identityName;
            DistinguishedName = distinguishedName;
            ACEId = aCEId;
            SID = sID;
            Rights = rights;
            Type = type;
            FileSystemRight = fileSystemRight;
            IsInherited = isInherited;
            InheritanceFlags = inheritanceFlags;
            PropagationFlags = propagationFlags;
        }
    }
}