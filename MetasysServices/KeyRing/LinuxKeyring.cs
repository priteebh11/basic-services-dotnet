﻿using System;
using System.Runtime.InteropServices;
using System.Security;

namespace JohnsonControls.Metasys.BasicServices;


public class LinuxKeyring : ISecretStore
{
    public void AddOrReplacePassword(string server, string username, SecureString password)
    {
        throw new NotImplementedException();
    }

    public void AddPassword(string server, string username, SecureString password)
    {
        throw new NotImplementedException();
    }

    public bool TryGetPassword(string server, string username, out SecureString password)
    {
        throw new NotImplementedException();
    }
}