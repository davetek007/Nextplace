﻿namespace Nextplace.Api.Models;

public class UserFavorite(string nextplaceId)
{
  public string NextplaceId { get; } = nextplaceId;
  
  public Property? Property { get; set; } 
}