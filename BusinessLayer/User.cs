using MonsterTradingCardGame.DataLayer;
using MonsterTradingCardGame.Routing;

namespace MonsterTradingCardGame.BusinessLayer
{
    public class User
    {
        private readonly Parser _parser = new();
        private readonly Tokens _token = new();
        private readonly Response _response = new();
        private readonly UserRepo _userMan = new();

        //user data
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; } 
        public string Token { get; set; }
        
        //profile data
        public string ProfileName { get; set; }
        public string Bio { get; set; } 
        public string Image { get; set; }


        //----------------------LOGIN----------------------
        public async Task Login(string body, StreamWriter writer)
        {
            Console.WriteLine("** inside login function **");//debug

            var (username, password) = _parser.UserDataParse(body, writer);

            bool Valid = await _userMan.GetUser(username, password);//checks if the user is valid
            if (Valid)
            {
                int? userId = await _userMan.GetUserId(username);
                Console.WriteLine($"** user id gotten : {userId} **");//debug
                string token = await _token.GetToken(userId.Value, username, writer);
                Console.WriteLine($"** token gotten : {token} **");//debug
            }
            else
            {
                await _response.HttpResponse(401, "Login Failed", writer);
            }
        }

        //----------------------REGISTRATION----------------------
        public async Task Register(string body, StreamWriter writer)
        {
            Console.WriteLine("** inside register **");//debug

            var (username, password) = _parser.UserDataParse(body, writer);

            Console.WriteLine($"** the credentioals inside the registr function name : {username}, pass : {password}  **");//debug

            bool Valid = await _userMan.InsertUser(username, password, writer);
            if (Valid)
            {
                Console.WriteLine("** isnide valid if statement for register **");//debug
                await _response.HttpResponse(201, "Successfully Registered", writer);
            }//errors are handeld in the userrepo specifically database access methods to show error more clearly
        }

        //----------------------PROFILE----------------------
        public async Task Profile(User user, StreamWriter writer)//gets profile
        {
            Console.WriteLine($"** inside get profile function **");//debug
            if (await _userMan.GetProfile(user, writer))//if the profile is found
            {
                await _response.HttpResponse(200, "Profile:", writer);
                writer.WriteLine($"Name: {user.ProfileName}");
                writer.WriteLine($"Bio: {user.Bio}");
                writer.WriteLine($"Image: {user.Image}");
            }
            else
            {
                await _response.HttpResponse(404, "User profile not found", writer);
            }
        }

        public async Task EditProfile(string body, User user,StreamWriter writer)//updates profile
        {
            Console.WriteLine($"** inside edit profile function **");//debug
            List<string> parsedData = _parser.ProfileDataParse(body, writer);

            if (parsedData.Count == 3)//if all data is there the profile can be updated
            {
                user.ProfileName = parsedData[0];
                user.Bio = parsedData[1];
                user.Image = parsedData[2];
                Console.WriteLine($" ID  :  {user.UserId}");
                Console.WriteLine($" Name  :  {user.ProfileName}");
                Console.WriteLine($" Bio  :  {user.Bio}");
                Console.WriteLine($" Image  :  {user.Image}");
                await _userMan.UpdateProfile(user, writer);
            }
            
            else
            {
                await _response.HttpResponse(400, "Profile could not be updated", writer);
            }
        }
    }
}
