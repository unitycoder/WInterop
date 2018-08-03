﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WInterop.Com.Types;
using WInterop.Registry.Types;
using WInterop.ErrorHandling;
using WInterop.Handles.Types;
using WInterop.Shell.Types;
using WInterop.Support;
using WInterop.Support.Buffers;
using WInterop.Authorization;

namespace WInterop.Shell
{
    public static partial class ShellMethods
    {
        public static IPropertyDescriptionList GetPropertyDescriptionListFromString(string value)
        {
            HRESULT result = Imports.PSGetPropertyDescriptionListFromString(value, new Guid(InterfaceIds.IID_IPropertyDescriptionList), out IPropertyDescriptionList list);
            if (result != HRESULT.S_OK)
                throw Errors.GetIoExceptionForHResult(result);
            return list;
        }

        public static RegistryKeyHandle AssocQueryKey(ASSOCF flags, ASSOCKEY key, string association, string extraInfo)
        {
            HRESULT result = Imports.AssocQueryKeyW(flags, key, association, extraInfo, out RegistryKeyHandle handle);
            if (result != HRESULT.S_OK)
                throw Errors.GetIoExceptionForHResult(result);
            return handle;
        }

        public static string AssocQueryString(ASSOCF flags, ASSOCSTR @string, string association, string extraInfo)
        {
            return BufferHelper.BufferInvoke((StringBuffer buffer) =>
            {
                flags |= ASSOCF.NoTruncate;

                HRESULT result;
                uint count = buffer.CharCapacity;
                while ((result = Imports.AssocQueryStringW(flags, @string, association, extraInfo, buffer, ref count)) == HRESULT.E_POINTER)
                {
                    buffer.EnsureCharCapacity(count);
                }

                if (result != HRESULT.S_OK)
                    throw Errors.GetIoExceptionForHResult(result, association);

                // Count includes the null
                buffer.Length = count - 1;
                return buffer.ToString();
            });
        }

        /// <summary>
        /// Get the path for the given known folder Guid.
        /// </summary>
        public static string GetKnownFolderPath(Guid folderIdentifier, KNOWN_FOLDER_FLAG flags = KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT)
        {
            HRESULT hr = Imports.SHGetKnownFolderPath(folderIdentifier, flags, EmptySafeHandle.Instance, out string path);
            if (hr != HRESULT.S_OK)
                throw Errors.GetIoExceptionForHResult(hr, folderIdentifier.ToString());

            return path;
        }

        /// <summary>
        /// Get the Shell item id for the given known folder Guid.
        /// </summary>
        public static ItemIdList GetKnownFolderId(Guid folderIdentifier, KNOWN_FOLDER_FLAG flags = KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT)
        {
            HRESULT hr = Imports.SHGetKnownFolderIDList(folderIdentifier, flags, EmptySafeHandle.Instance, out ItemIdList id);
            if (hr != HRESULT.S_OK)
                throw Errors.GetIoExceptionForHResult(hr, folderIdentifier.ToString());

            return id;
        }

        /// <summary>
        /// Get the name for a given Shell item ID.
        /// </summary>
        public static string GetNameFromId(ItemIdList id, SIGDN form = SIGDN.NORMALDISPLAY)
        {
            HRESULT hr = Imports.SHGetNameFromIDList(id, form, out string name);
            if (hr != HRESULT.S_OK)
                throw Errors.GetIoExceptionForHResult(hr);

            return name;
        }

        /// <summary>
        /// Get the IKnownFolderManager.
        /// </summary>
        public static IKnownFolderManager GetKnownFolderManager()
        {
            return (IKnownFolderManager)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid(ClassIds.CLSID_KnownFolderManager)));
        }

        /// <summary>
        /// Get the Guid identifiers for all known folders.
        /// </summary>
        public unsafe static IEnumerable<Guid> GetKnownFolderIds()
        {
            List<Guid> ids = new List<Guid>();

            IKnownFolderManager manager = GetKnownFolderManager();
            uint count = manager.GetFolderIds(out SafeComHandle buffer);

            using (buffer)
            {
                Guid* g = (Guid*)buffer.DangerousGetHandle();
                for (int i = 0; i < count; i++)
                    ids.Add(*g++);
            }

            return ids;
        }

        /// <summary>
        /// Collapses common path segments into the equivalent environment string.
        /// Returns null if unsuccessful.
        /// </summary>
        public static string UnexpandEnvironmentStrings(string path)
        {
            return BufferHelper.BufferInvoke((StringBuffer buffer) =>
            {
                buffer.EnsureCharCapacity(Paths.MaxPath);
                if (!Imports.PathUnExpandEnvStringsW(path, buffer, buffer.CharCapacity))
                    return null;

                buffer.SetLengthToFirstNull();
                return buffer.ToString();
            });
        }

        /// <summary>
        /// Expands environment variables for the given user token. If the token is
        /// null, returns the system variables.
        /// </summary>
        public static string ExpandEnvironmentVariablesForUser(AccessToken token, string value)
        {
            return BufferHelper.BufferInvoke((StringBuffer buffer) =>
            {
                while (!Imports.ExpandEnvironmentStringsForUserW(token, value, buffer, buffer.CharCapacity))
                {
                    Errors.ThrowIfLastErrorNot(WindowsError.ERROR_INSUFFICIENT_BUFFER);
                    buffer.EnsureCharCapacity(buffer.CharCapacity * 2);
                }

                buffer.SetLengthToFirstNull();
                return buffer.ToString();
            });
        }
    }
}
