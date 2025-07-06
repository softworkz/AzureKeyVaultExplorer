﻿// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for license information. 

namespace Microsoft.Vault.Explorer.Common
{
    using System;

    public class AddFileEventArgs : EventArgs
    {
        public readonly string FileName;

        public AddFileEventArgs(string filename)
        {
            this.FileName = filename;
        }
    }
}
