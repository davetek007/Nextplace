using Microsoft.Graph.Models;

namespace Nextplace.Functions.Models;

public class EmailContent
{
    private static readonly string PropertyValuationTemplate = @"<!doctype html>
<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"">
<head>
	<title>Nextplace</title>
	<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
	<meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
	<meta name=""viewport"" content=""width=device-width, initial-scale=1.0""> 
	<link href=""https://fonts.googleapis.com/css2?family=Montserrat:wght@100;300;400;500;600;700;800;900&display=swap"" rel=""stylesheet"">
    <link href=""https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;500;600;700;800;900&display=swap"" rel=""stylesheet"">
    <link href=""https://fonts.googleapis.com/css2?family=Rajdhani:wght@300;400;500;600;700&display=swap"" rel=""stylesheet"">
    
	<style type=""text/css"">
		#outlook a {
			padding: 0;
		}

		.ReadMsgBody {
			width: 100%;
		}

		.ExternalClass {
			width: 100%;
		}

			.ExternalClass * {
				line-height: 100%;
			}

		body {
			margin: 0;
			padding: 0;
			-webkit-text-size-adjust: 100%;
			-ms-text-size-adjust: 100%;
		}

		table, td {
			border-collapse: collapse;
			mso-table-lspace: 0pt;
			mso-table-rspace: 0pt;
		}

		img {
			border: 0;
			height: auto;
			line-height: 100%;
			outline: none;
			text-decoration: none;
			-ms-interpolation-mode: bicubic;
		}

		p {
			display: block;
			margin: 13px 0;
		}
	</style>
	<style type=""text/css"">
		@media only screen and (max-width:480px) {
			@-ms-viewport {
				width: 320px;
			}

			@viewport {
				width: 320px;
			}
		}
	</style>
	<style type=""text/css"">
		@media only screen and (min-width:480px) {
			.mj-column-per-66 {
				width: 66.66666666666666% !important;
			}

			.mj-column-per-33 {
				width: 33.33333333333333% !important;
			}

			.mj-column-per-100 {
				width: 100% !important;
			}
		}
	</style>
</head>
<body style=""background: white;"">

	<div class=""mj-container"" style=""background-color:white;"">
		<div style=""margin:0px auto;max-width:600px;"">
			<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""font-size:0px;width:100%"" align=""center"" border=""0"">
				<tbody>
					<tr>
						<td style=""text-align:center;vertical-align:top;direction:ltr;font-size:0px;padding:20px 0px 20px 0px;"">
							<div class=""mj-column-per-66 outlook-group-fix"" style=""vertical-align:top;display:inline-block;direction:ltr;font-size:13px;text-align:left;width:100%;""><table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" border=""0""><tbody><tr><td style=""word-wrap:break-word;font-size:0px;padding:0px 0px 0px 25px;padding-top:0px;padding-bottom:0px;"" align=""left""><div style=""cursor:auto;color:#5E6977;font-family:Arial, sans-serif;font-size:11px;line-height:22px;text-align:left;""><p style=""margin: 10px 0;""></p></div></td></tr></tbody></table></div>
						</td>
					</tr>
				</tbody>
			</table>
		</div>
		<div style=""margin:0px auto;max-width:600px;background:#fdf5e6;"">
			<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""font-size:0px;width:100%;background:#fdf5e6;"" align=""center"" border=""0"">
				<tbody>
					<tr>
						<td style=""text-align:center;vertical-align:top;direction:ltr;font-size:0px;padding:0px 0px 0px 0px;padding-bottom:40px;padding-top:40px;"">
							<div class=""mj-column-per-100 outlook-group-fix"" style=""vertical-align:top;display:inline-block;direction:ltr;font-size:13px;text-align:left;width:100%;""><table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""vertical-align:top;"" width=""100%"" border=""0""><tbody><tr><td style=""word-wrap:break-word;font-size:0px;padding:0px 0px 0px 0px;padding-top:10px;padding-bottom:10px;padding-right:25px;padding-left:25px;"" align=""center""><table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;border-spacing:0px;"" align=""center"" border=""0""><tbody><tr><td style=""width:202px;"">
								<img alt="""" title="""" height=""auto"" src=""cid:logo"" style=""border:none;border-radius:;display:block;font-size:13px;outline:none;text-decoration:none;width:100%;height:auto;"" width=""202""></td></tr></tbody></table></td></tr></tbody></table></div>
						</td>
					</tr>
				</tbody>
			</table>
		</div>
		<div style=""margin:0px auto;max-width:600px;background:#fdf5e6;"">
			<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""font-size:0px;width:100%;background:#fdf5e6;border-top-color:black;border-left-color:#fdf5e6;border-right-color:#fdf5e6;border-bottom-color:#fdf5e6;"" align=""center"" border=""0"">
				<tbody>
					<tr>
						<td style=""text-align:center;vertical-align:top;direction:ltr;font-size:0px;padding:0px 0px 0px 0px;padding-bottom:20px;padding-top:0px;"">
							<div class=""mj-column-per-100 outlook-group-fix"" style=""vertical-align:top;display:inline-block;direction:ltr;font-size:13px;text-align:left;width:100%;"">
								<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""vertical-align:top;"" width=""100%"" border=""0"">
									<tbody>
										<tr>
											<td style=""word-wrap:break-word;font-size:0px;padding:0px 0px 0px 0px;padding-top:10px;padding-bottom:0px;padding-right:25px;padding-left:25px;"" align=""left"">
												<div style=""cursor:auto;color:#5E6977;font-family:Arial, sans-serif;font-size:13px;line-height:18px;text-align:left;"">
													<style></style><h1 style=""""><span style=""font-family:Playfair Display,Times New Roman;font-weight:300;font-size:48px""><span style=""color:black"">[Title]</span></span></h1>
												</div>
											</td>
										</tr>
										<tr>
											<td style=""word-wrap:break-word;font-size:0px;padding:0px 0px 0px 0px;padding-top:0px;padding-bottom:10px;padding-right:25px;padding-left:25px;"" align=""left"">
												<div style=""cursor:auto;color:#5E6977;font-family:Arial, sans-serif;font-size:13px;line-height:22px;text-align:left;"">
													<style></style><p style=""margin: 10px 0;""><span style=""font-family:Times New Roman;""><span style=""color:black;""><span style=""font-size:15px;"">
                                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" border=""0"">
                                    <tbody>
                                        <tr>
                                            <td style=""word-wrap:break-word;font-size:0px;padding:0px 25px;"" align=""left"">
                                                <div style=""cursor:auto;color:#5E6977;font-family:Arial, sans-serif;font-size:13px;line-height:22px;text-align:left;"">                                                   
                                                    
                                                    <h2 style=""font-family:Rajdhani,sans-serif;;font-size:32px;font-weight:400; margin-top:30px;color:black"">Valuation Details</h2>
													<p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:18.4px"">You valued your property at [PredictedValue]. Our miners valued it as follows.</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px;margin-top:30px"">Min Value: [Min]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">Max Value: [Max]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">Average Value: [Avg]</p>         
													
													<p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:18.4px;margin-top:30px"">Based on this, you [UnderOver] your property.</p>
                                                    
													
													<h2 style=""font-family:Rajdhani,sans-serif;;font-size:32px;font-weight:400; margin-top:60px;color:black"">Property Details</h2>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">Address: [address]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">City: [city]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">State: [state]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">Zip Code: [zipCode]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px;margin-top:40px"">Number of Beds: [numberOfBeds]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">Number of Baths: [numberOfBaths]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">Square Feet: [squareFeet]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">Lot Size: [lotSize]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">Year Built: [yearBuilt]</p>
                                                    <p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:16.8px"">HOA Dues: [hoaDues]</p>                                           
                                                </div>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
													<br />
													<br />
													
													<br /><br /><br />
													</span></span></span>
													
													<p style=""font-family:Montserrat, sans-serif;font-weight:400;color:black;font-size:18.4px;margin-top:30px"">Thanks, the Nextplace Team</p>
												
												</div>
											</td>
										</tr></tbody></table></td></tr>
									</tbody>
								</table>
							</div>
						</td>
					</tr>
				</tbody>
			</table>
		</div>
		<div style=""margin:0px auto;max-width:600px;"">
			<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""font-size:0px;width:100%;"" align=""center"" border=""0"">
				<tbody>
					<tr>
						<td style=""text-align:center;vertical-align:top;direction:ltr;font-size:0px;padding:20px 0px 20px 0px;"">
						</td>
					</tr>
				</tbody>
			</table>
		</div>
	</div>
</body>
</html>";

    internal static string PropertyValuation(string predictedValue, string minValue, string maxValue, string avgValue, string city, string state, 
        string zipCode, string address, string numberOfBeds, string numberOfBaths, string squareFeet, 
        string lotSize, string yearBuilt, string hoaDues, string underOver)
    {
        return PropertyValuationTemplate
            .Replace("[Title]", @"Your Property Valuation")
            .Replace("[PredictedValue]", predictedValue)
            .Replace("[UnderOver]", underOver)
            .Replace("[Min]", minValue)
            .Replace("[Max]", maxValue)
            .Replace("[Avg]", avgValue)
            .Replace("[city]", city)
            .Replace("[state]", state)
            .Replace("[zipCode]", zipCode)
            .Replace("[address]", address)
            .Replace("[numberOfBeds]", numberOfBeds)
            .Replace("[numberOfBaths]", numberOfBaths)
            .Replace("[squareFeet]", squareFeet)
            .Replace("[lotSize]", lotSize)
            .Replace("[yearBuilt]", yearBuilt)
            .Replace("[hoaDues]", hoaDues);
    }

    public static void AddHeaderImage(Message msg)
    {
        const string b64Image = "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";

        msg.Attachments = new List<Attachment>();
        var attachment = new FileAttachment
        {
            Name = "image.jpg",
            ContentBytes = Convert.FromBase64String(b64Image),
            ContentType = "image/jpeg",
            ContentId = "logo",
            IsInline = true
        };

        msg.Attachments.Add(attachment);
    }
}