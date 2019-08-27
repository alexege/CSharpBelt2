using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CSharpBelt2.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CSharpBelt2.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;
		// here we can "inject" our context service into the constructor
		public HomeController(MyContext context)
		{
			dbContext = context;
		}

        public IActionResult Index()
        {
            return View();
        }

        // Let a new user login
        [HttpPost("Login")]
        public IActionResult Login(LogUser logUser)
        {
            if(ModelState.IsValid)
            {
                // Look to see if user exists in database
                // var found_user = dbContext.Users.FirstOrDefault(user => user.Email == logUser.LogEmail);
                var found_user = dbContext.Users.FirstOrDefault(user => user.UserName == logUser.LogUserName);

                // If no user found via that email address, display error and redirect back to index page.
                if(found_user == null)
                {
                    ModelState.AddModelError("LogUserName", "Incorrect Email or Password");
                    return View("Index");
                }

                //If a user is found, Verify their password to the hashed password stored in the database.
                PasswordHasher<LogUser> Hasher = new PasswordHasher<LogUser>();
                var user_verified = Hasher.VerifyHashedPassword(logUser, found_user.Password, logUser.LogPassword);

                //If VerifyHashedPassword returns a 0, Passwords didn't match. Return user to Index.
                if(user_verified == 0)
                {
                    ModelState.AddModelError("LogUserName", "Incorrect UserName or Password");
                    return View("Index");
                }

                //Store logged in user's id into session.
                HttpContext.Session.SetInt32("UserId", found_user.UserId);

                //Store logged in user's id into ViewBag.
                ViewBag.Logged_in_user_id = found_user.UserId;

                return RedirectToAction("Dashboard");
            }
            return View("Index");
        }

        //Register a new user
        [HttpPost("Register")]
        public IActionResult Register(User newUser)
        {
            //If ModelState contains no errors
            if(ModelState.IsValid)
            {
                //Check to see if UserName address already exists in database
                bool notUnique = dbContext.Users.Any(a => a.UserName == newUser.UserName);

                //If UserName already taken,display error and redirect to index.
                if(notUnique)
                {
                    ModelState.AddModelError("UserName", "UserName already in use. Please use a new one.");
                    return View("Index");
                }

                //If unique password, hash the new user's password
                PasswordHasher<User> hasher = new PasswordHasher<User>();
                string hash = hasher.HashPassword(newUser, newUser.Password);
                newUser.Password = hash;

                dbContext.Users.Add(newUser);
                dbContext.SaveChanges();

                //Store new user's id into session
                var last_added_User = dbContext.Users.Last().UserId;
                HttpContext.Session.SetInt32("UserId", last_added_User);
            
                return RedirectToAction("Dashboard");
            }
        return View("Index");
        }

        //Navigate to the Dashboard on successful Login/Registration
        [HttpGet("Dashboard")]
        public IActionResult Dashboard()
        {
            //Checked to see if user is in session or not. If not, redirec to index.
            if(HttpContext.Session.GetInt32("UserId") == null){
                return View("Index");
            }

            //Get user id from session
            int? UserId = HttpContext.Session.GetInt32("UserId");

            //If no user in session, redirect to index
            if(UserId == null)
            {
                return View("Index");
            }

            //Place current logged in user's name in Viewbag.FirstName
            var current_user = dbContext.Users.First(usr => usr.UserId == UserId);
            ViewBag.FirstName = current_user.FirstName;

            //Place current logged in user's id in Viewbag.Logged_in_user_id
            ViewBag.Logged_in_user_id = HttpContext.Session.GetInt32("UserId");

            var hobbies = dbContext.Hobbies
                            .Include(a => a.Enthusiasts)
                                .ThenInclude(a => a.User)
                            .Include(a => a.Creator)
                            .ToList();

            return View("Dashboard", hobbies);
        }

        [HttpGet("Hobby/New")]
        public IActionResult newHobby()
        {
            return View();
        }

        [HttpPost("Hobby/New")]
        public IActionResult createHobby(Hobby newHobby)
        {
            //If no user logged in, redirect to Index page
            if(HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Index");
            }

            //Check if form is correct
            if(ModelState.IsValid)
            {
                //Check to see if Hobby name already exists in database
                if(dbContext.Hobbies.Any(u => u.Name == newHobby.Name))
                {
                    ModelState.AddModelError("Name", "Hobby already exists!");
                    return View("newHobby");
                }

                var hobby_creator_id = HttpContext.Session.GetInt32("UserId");
                
                //Set logged in user as creator
                newHobby.UserId = (int)hobby_creator_id;
                
                //Add new hobby to list of hobbies
                dbContext.Hobbies.Add(newHobby);

                dbContext.SaveChanges();

                // return RedirectToAction("Dashboard", new {HobbyId = newHobby.UserId});
                return RedirectToAction("Dashboard");
            }

            return View("newHobby");
        }

        [HttpGet("Hobby/{HobbyId}")]
        public IActionResult ShowHobby(int HobbyId)
        {
            Hobby selected_hobby = dbContext.Hobbies.Include(h => h.Creator)
                                                    .Include(h => h.Enthusiasts)
                                                        .ThenInclude(u => u.User)
                                                    .FirstOrDefault(hob => hob.HobbyId == HobbyId);

            var current_user = HttpContext.Session.GetInt32("UserId");
            ViewBag.current_user = current_user;

            return View(selected_hobby);                 
        }

        [HttpGet("Hobby/Edit/{HobbyId}")]
        public IActionResult EditHobby(int HobbyId)
        {
            //Grab Hobby matching passed in id to populate fields
            Hobby selected_hobby = dbContext.Hobbies.Include(h => h.Enthusiasts)
                                                        .ThenInclude(u => u.User)
                                                    .FirstOrDefault(hob => hob.HobbyId == HobbyId);
            return View(selected_hobby);               
        }

        [HttpPost("Hobby/Update/{HobbyId}")]
        public IActionResult UpdateHobby(Hobby edit_hobby, int HobbyId)
        {
            //Grab Hobby matching passed in id to populate fields
            Hobby selected_hobby = dbContext.Hobbies.Include(h => h.Enthusiasts)
                                                        .ThenInclude(u => u.User)
                                                    .FirstOrDefault(hob => hob.HobbyId == HobbyId);
            if(ModelState.IsValid){

                //Update the selected Hobby's attributes
                selected_hobby.Name = edit_hobby.Name;
                selected_hobby.Description = edit_hobby.Description;
                dbContext.SaveChanges();

                return RedirectToAction("Dashboard");  
            }
            
            return View("EditHobby", selected_hobby);   
        }

        [HttpGet("AddToHobbies/{HobbyId}")]
        public IActionResult AddToHobbies(int HobbyId)
        {
            var current_user = (int)HttpContext.Session.GetInt32("UserId");

            Enthusiast newEnthusiast = new Enthusiast(HobbyId, current_user);

            //newEnthusiast.User.Proficiency =

            dbContext.Enthusiasts.Add(newEnthusiast);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }
        
        //Add to hobbies with difficulty
        [HttpPost("AddToHobbies/{HobbyId}")]
        public IActionResult AddToHobbiesLevel(int HobbyId)
        {
            var current_user = (int)HttpContext.Session.GetInt32("UserId");

            Enthusiast newEnthusiast = new Enthusiast(HobbyId, current_user);

            //This is where the logic would go to add a users's proficiency
            //newEnthusiast.User.Proficiency = //result of selection

            dbContext.Enthusiasts.Add(newEnthusiast);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        [HttpGet("RemoveFromHobbies/{HobbyId}")]
        public IActionResult RemoveFromHobbies(int HobbyId)
        {
            //Grab list of Enthusiasts for specific hobby
            var hobby_enthusiasts = dbContext.Enthusiasts.Where(enth => enth.HobbyId == HobbyId).ToList();

            //Grab current logged in user
            var current_user = (int)HttpContext.Session.GetInt32("UserId");

            //Get Enthusiast to remove
            Enthusiast leaving_enthusiast = hobby_enthusiasts.FirstOrDefault(leaving => leaving.UserId == current_user);

            dbContext.Enthusiasts.Remove(leaving_enthusiast);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }

         //Delete an Activity
        [HttpGet("Hobby/{HobbyId}/delete")]
        public IActionResult DeleteHobby(int HobbyId)
        {
            // Grab the hobby from the list of hobbies
            Hobby hobby = dbContext.Hobbies.Include(hob => hob.Enthusiasts).FirstOrDefault(h => h.HobbyId == HobbyId);

            //Get list of Enthusiasts from the Hobby
            var list_of_enthusiasts = hobby.Enthusiasts.ToList();

            foreach(var enthusiast in list_of_enthusiasts)
            {
                dbContext.Enthusiasts.Remove(enthusiast);
            }

            //Delete the Activity from the list of activities
            dbContext.Hobbies.Remove(hobby);
            dbContext.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        //Log a user out of session
        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            //Destroy Session
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
