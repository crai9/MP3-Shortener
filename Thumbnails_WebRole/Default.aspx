<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Thumbnails_WebRole._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>MP3 Shortener</title>

</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="sm1" runat="server" />
        <h1>MP3 Shortener</h1>
        <div style="margin-bottom: 10px; display:table-cell;">
            Upload mp3 file:
            <asp:FileUpload ID="upload" runat="server" />
            
            <asp:Button ID="submitButton" runat="server" Text="Submit" OnClick="submitButton_Click" />
            <img alt="loading" height="21px" id="loadingImage" style="display:none" src="http://i.imgur.com/PO9WnXJ.gif" />
        </div>
        <div>
            <asp:UpdatePanel ID="up1" runat="server" style="margin-top:10px">
                <ContentTemplate>
                    <asp:ListView ID="ThumbnailDisplayControl" runat="server">
                        <LayoutTemplate>
                            <asp:Image ID="itemPlaceholder" runat="server" />
                        </LayoutTemplate>
                        <ItemTemplate>
                            <div class="audio-div" style="margin-bottom: 3px">

                                <audio controls>
                                    <source src="<%# Eval("Url") %>" type="audio/mpeg">
                                    Can't use audio element on your browser
                                </audio>
                                <br />
                                <a target="_blank" href="<%# Eval("Url") %>">Download File</a><br />
                                <label>Name: <%# Eval("Name") %><br /> Shortened on Instance: <%# Eval("Instance") %></label>
                                
                            </div>
                        </ItemTemplate>
                    </asp:ListView>
                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </form>
    <script type="text/javascript">

        document.getElementById('submitButton').addEventListener("click", function () {
            document.getElementById('loadingImage').style.display = "inline";
        });

    </script>
</body>
</html>
