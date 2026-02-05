using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class LegalAgreementViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isAccepted;

    public string TermsContent => @"
BUSINESS SUITE - SOFTWARE LICENSE AGREEMENT & TERMS OF SERVICE

This User License Agreement ('Agreement') is a legal agreement between you (either an individual or a single entity) and Al Anwar Studio (represented by Murtaza Patel), the developer of the Business Suite software.

1. OWNERSHIP AND COPYRIGHT
The Business Suite software ('Software') is owned by Al Anwar Studio. All intellectual property rights, including copyrights, patents, trade secrets, and trademarks, are reserved. You are granted a non-exclusive, non-transferable license to use the Software.

2. LICENSE GRANT
Al Anwar Studio grants you the right to install and use one copy of the Software on a single computer. You may not distribute, sell, lease, or sublicense the Software to any third party.

3. RESTRICTIONS
You shall not:
- Reverse engineer, decompile, or disassemble the Software.
- Modify the Software or create derivative works based upon the Software.
- Remove any proprietary notices or labels on the Software.

4. NO WARRANTY
THE SOFTWARE IS PROVIDED 'AS IS' WITHOUT WARRANTY OF ANY KIND. AL ANWAR STUDIO DISCLAIMS ALL WARRANTIES, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.

5. LIMITATION OF LIABILITY
IN NO EVENT SHALL AL ANWAR STUDIO OR MURTAZA PATEL BE LIABLE FOR ANY DAMAGES WHATSOEVER (INCLUDING, WITHOUT LIMITATION, DAMAGES FOR LOSS OF BUSINESS PROFITS, BUSINESS INTERRUPTION, LOSS OF BUSINESS INFORMATION, OR ANY OTHER PECUNIARY LOSS) ARISING OUT OF THE USE OF OR INABILITY TO USE THIS PRODUCT.

6. DATA RESPONSIBILITY
The Software is a tool for managing business data. Users are solely responsible for performing regular backups of their data using the provided backup tools. Al Anwar Studio is not responsible for any data loss, corruption, or inaccuracies in billing/GST reporting.

7. GOVERNING LAW
This Agreement is governed by the laws of India. Any disputes shall be subject to the exclusive jurisdiction of the courts in [Your City/State], India.

BY CLICKING 'I AGREE' OR INSTALLING THE SOFTWARE, YOU ACKNOWLEDGE THAT YOU HAVE READ THIS AGREEMENT, UNDERSTAND IT, AND AGREE TO BE BOUND BY ITS TERMS AND CONDITIONS.

Developed by: Al Anwar Studio
Lead Developer: Murtaza Patel
";

    public event Action? OnAccepted;

    [RelayCommand]
    private void Accept()
    {
        if (IsAccepted)
        {
            OnAccepted?.Invoke();
        }
    }

    [RelayCommand]
    private void Decline()
    {
        Environment.Exit(0);
    }
}
