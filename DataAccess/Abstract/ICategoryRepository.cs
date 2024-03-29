﻿using Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Abstract
{
    public interface ICategoryRepository:IRepository<Category>
    {
        Category GetByIdWithBlogs(int id);
    }
}
