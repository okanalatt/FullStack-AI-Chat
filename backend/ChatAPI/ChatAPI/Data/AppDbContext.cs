﻿
using ChatAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {
        }
        public DbSet<Message>Messages { get; set; }
    }
}
