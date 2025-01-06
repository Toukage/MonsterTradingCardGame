namespace MonsterTradingCardGame.DataLayer
{
    public class Response
    {
        public async Task HttpResponse(int Statuscode, string mssg, StreamWriter writer)//writes the response
        {
            string Status = GetStatus(Statuscode);
            writer.WriteLine($"HTTP/1.1 {Statuscode} {Status}.");
            writer.WriteLine("Content-Type: text/plain");
            writer.WriteLine();
            writer.WriteLine($"{mssg}");
        }

        static string GetStatus(int Statuscode)//all status codes
        {
            string status;
            switch (Statuscode)
            {
                case 400:
                    status = "Bad Request";
                    break;
                case 401:
                    status = "Unauthorized";
                    break;
                case 403:
                    status = "Forbidden";
                    break;
                case 404:
                    status = "Not Found";
                    break;
                case 409:
                    status = "Conflict";
                    break;
                case 200:
                    status = "OK";
                    break;
                case 201:
                    status = "Created";
                    break;
                case 500:
                    status = "Internal Server Error";
                    break;
                default:
                    status = "Unknown Status";
                break;
            }
            return status;
        }
    }
}
