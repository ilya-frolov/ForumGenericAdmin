using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dino.CoreMvc.Admin.Logic;

namespace Dino.CoreMvc.Admin.Models.Admin
{
    public abstract class BaseAdminModel
    {
        /// <summary>
        /// Custom pre-map from database model.
        /// </summary>
        /// <param name="dbModel">The database model.</param>
        /// <param name="model">The model.</param>
        /// <param name="context">The mapping context.</param>
        /// <returns>True if the mapping should continue, false otherwise.</returns>
        public virtual bool CustomPreMapFromDbModel(dynamic dbModel, dynamic model, ModelMappingContext context)
        {
            return true;
        }

        /// <summary>
        /// Custom pre-map to database model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="dbModel">The database model.</param>
        /// <param name="context">The mapping context.</param>
        /// <returns>True if the mapping should continue, false otherwise.</returns>
        public virtual bool CustomPreMapToDbModel(dynamic model, dynamic dbModel, ModelMappingContext context)
        {
            return true;
        }

        /// <summary>
        /// Custom post-map from database model.
        /// </summary>
        /// <param name="dbModel">The database model.</param>
        /// <param name="model">The model.</param>
        /// <param name="context">The mapping context.</param>
        public virtual void CustomPostMapFromDbModel(dynamic dbModel, dynamic model, ModelMappingContext context)
        {
        }

        /// <summary>
        /// Custom post-map to database model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="dbModel">The database model.</param>
        /// <param name="context">The mapping context.</param>
        public virtual void CustomPostMapToDbModel(dynamic model, dynamic dbModel, ModelMappingContext context)
        {
        }
    }
}
