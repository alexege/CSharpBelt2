using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CSharpBelt2.Models
{
    public class Enthusiast
    {
        [Key]
        public int EnthusiastId { get; set; }

        public int HobbyId { get; set; }

        public int UserId { get; set; }

        public bool Joined { get; set; }

        public string Difficulty { get; set; }

        public Enthusiast(int HobbyId, int UserId)
        {
            this.HobbyId = HobbyId;
            this.UserId = UserId;
            this.Joined = true;
        }

        //Navigation Property
        public Hobby Hobby { get; set; }
        public User User { get; set; }
    }
}