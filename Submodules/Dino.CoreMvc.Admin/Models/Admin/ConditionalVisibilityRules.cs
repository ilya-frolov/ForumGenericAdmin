using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dino.CoreMvc.Admin.Models.Admin
{
    public class Condition
    {
        public string PropertyName { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public bool IsProperty { get; set; }
    }

    public class ConditionalVisibilityRules<TModel>
    {
        private readonly List<ConditionGroup<TModel>> _conditionGroups = new List<ConditionGroup<TModel>>();

        /// <summary>
        /// Add a new condition group to the rules set
        /// </summary>
        /// <returns>A new condition group</returns>
        public ConditionGroup<TModel> AddGroup()
        {
            var group = new ConditionGroup<TModel>();
            _conditionGroups.Add(group);
            return group;
        }

        /// <summary>
        /// Get all condition groups
        /// </summary>
        public List<ConditionGroup<TModel>> GetGroups()
        {
            return _conditionGroups;
        }
    }

    public class ConditionGroup<TModel>
    {
        private readonly List<string> _showProperties = new List<string>();
        private readonly List<string> _hideProperties = new List<string>();
        private ConditionNode _conditionTree;
        private List<Condition> _conditions = new List<Condition>();

        /// <summary>
        /// Define a condition with a predicate expression
        /// </summary>
        /// <param name="condition">Lambda expression for the condition</param>
        /// <returns>This condition group for chaining</returns>
        public ConditionGroup<TModel> When(Expression<Func<TModel, bool>> condition)
        {
            try
            {
                // Build the condition tree immediately
                _conditionTree = ConditionVisitor.ExtractConditions(condition);

                // Also build the legacy format for backward compatibility
                _conditions.Clear();
                BuildLegacyConditions(_conditionTree);

                return this;
            }
            catch (Exception ex)
            {
                // Create a default condition for error cases
                _conditions.Clear();
                _conditions.Add(new Condition
                {
                    PropertyName = "Id",
                    Operator = "!=",
                    Value = null,
                    IsProperty = false
                });
                return this;
            }
        }

        /// <summary>
        /// Builds the legacy format conditions from the tree
        /// </summary>
        private void BuildLegacyConditions(ConditionNode node)
        {
            if (node == null) return;

            if (node.NodeType == ConditionNodeType.Condition)
            {
                // Add leaf condition
                _conditions.Add(new Condition
                {
                    PropertyName = node.Property,
                    Operator = node.Operator,
                    Value = node.Value,
                    IsProperty = node.IsProperty
                });
            }
            else if (node.NodeType == ConditionNodeType.Group)
            {
                // Process children
                foreach (var child in node.Children)
                {
                    BuildLegacyConditions(child);
                }

                // Add marker for OR groups
                if (node.Rule == "OR" && node.Children.Count > 0)
                {
                    _conditions.Add(new Condition
                    {
                        PropertyName = "_operation",
                        Operator = "OR",
                        Value = null,
                        IsProperty = false
                    });
                }
            }
        }

        /// <summary>
        /// Define a condition using property names directly - useful for complex nested properties
        /// </summary>
        /// <param name="propertyName">Name of the property to check</param>
        /// <param name="value">The value to compare against</param>
        /// <param name="operatorType">The operator to use (defaults to ==)</param>
        /// <returns>This condition group for chaining</returns>
        public ConditionGroup<TModel> WhenProperty(string propertyName, object value, string operatorType = "==")
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

            try
            {
                // Create a condition node for this property condition
                _conditionTree = new ConditionNode
                {
                    NodeType = ConditionNodeType.Group,
                    Rule = "AND",
                    Children = new List<ConditionNode>
                    {
                        new ConditionNode
                        {
                            NodeType = ConditionNodeType.Condition,
                            Property = propertyName,
                            Operator = operatorType,
                            Value = value,
                            IsProperty = false
                        }
                    }
                };

                // Also build the legacy format for backward compatibility
                _conditions.Clear();
                _conditions.Add(new Condition
                {
                    PropertyName = propertyName,
                    Operator = operatorType,
                    Value = value,
                    IsProperty = false
                });

                return this;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing property condition: {ex.Message}");
                return this;
            }
        }

        /// <summary>
        /// Specify properties to show when the condition is true
        /// </summary>
        /// <param name="properties">Lambda expressions selecting properties to show</param>
        /// <returns>This condition group for chaining</returns>
        public ConditionGroup<TModel> Show(params Expression<Func<TModel, object>>[] properties)
        {
            foreach (var property in properties)
            {
                _showProperties.Add(GetPropertyName(property));
            }
            return this;
        }

        /// <summary>
        /// Specify properties to show by name when the condition is true
        /// </summary>
        /// <param name="propertyNames">Names of properties to show</param>
        /// <returns>This condition group for chaining</returns>
        public ConditionGroup<TModel> ShowProperties(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (!string.IsNullOrEmpty(propertyName))
                {
                    _showProperties.Add(propertyName);
                }
            }
            return this;
        }

        /// <summary>
        /// Specify properties to hide when the condition is true
        /// </summary>
        /// <param name="properties">Lambda expressions selecting properties to hide</param>
        /// <returns>This condition group for chaining</returns>
        public ConditionGroup<TModel> Hide(params Expression<Func<TModel, object>>[] properties)
        {
            foreach (var property in properties)
            {
                _hideProperties.Add(GetPropertyName(property));
            }
            return this;
        }

        /// <summary>
        /// Specify properties to hide by name when the condition is true
        /// </summary>
        /// <param name="propertyNames">Names of properties to hide</param>
        /// <returns>This condition group for chaining</returns>
        public ConditionGroup<TModel> HideProperties(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (!string.IsNullOrEmpty(propertyName))
                {
                    _hideProperties.Add(propertyName);
                }
            }
            return this;
        }

        /// <summary>
        /// Get the properties to show when the condition is true
        /// </summary>
        public List<string> GetShowProperties()
        {
            return _showProperties;
        }

        /// <summary>
        /// Get the properties to hide when the condition is true
        /// </summary>
        public List<string> GetHideProperties()
        {
            return _hideProperties;
        }

        /// <summary>
        /// Get the condition tree for this group
        /// </summary>
        public ConditionNode GetConditionTree()
        {
            return _conditionTree;
        }

        /// <summary>
        /// Get the legacy conditions for backward compatibility
        /// </summary>
        public List<Condition> GetConditions()
        {
            return _conditions;
        }

        /// <summary>
        /// Get property name from expression
        /// </summary>
        private string GetPropertyName(Expression<Func<TModel, object>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                // For nested properties, we need to build the path recursively
                if (memberExpression.Expression is MemberExpression innerMember)
                {
                    string parentPath = ExtractNestedPath(memberExpression.Expression);
                    if (!string.IsNullOrEmpty(parentPath))
                    {
                        return $"{parentPath}.{memberExpression.Member.Name}";
                    }
                }

                return memberExpression.Member.Name;
            }
            if (expression.Body is UnaryExpression unaryExpression &&
                unaryExpression.Operand is MemberExpression unaryMemberExpression)
            {
                // For nested properties in unary expressions
                if (unaryMemberExpression.Expression is MemberExpression innerMember)
                {
                    string parentPath = ExtractNestedPath(unaryMemberExpression.Expression);
                    if (!string.IsNullOrEmpty(parentPath))
                    {
                        return $"{parentPath}.{unaryMemberExpression.Member.Name}";
                    }
                }

                return unaryMemberExpression.Member.Name;
            }
            throw new ArgumentException("Expression must be a property access", nameof(expression));
        }

        /// <summary>
        /// Helper method to extract nested property paths (instance method)
        /// </summary>
        private string ExtractNestedPath(Expression expr)
        {
            if (expr is MemberExpression memberExpression)
            {
                if (memberExpression.Expression is ParameterExpression)
                {
                    // Root property
                    return memberExpression.Member.Name;
                }
                else if (memberExpression.Expression is MemberExpression innerMember)
                {
                    // Nested property
                    string parentPath = ExtractNestedPath(memberExpression.Expression);
                    if (!string.IsNullOrEmpty(parentPath))
                    {
                        return $"{parentPath}.{memberExpression.Member.Name}";
                    }
                }

                return memberExpression.Member.Name;
            }

            if (expr is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            {
                return ExtractNestedPath(unary.Operand);
            }

            return null;
        }
    }

    public class ConditionVisitor : ExpressionVisitor
    {
        public List<Condition> Conditions { get; } = new List<Condition>();
        private readonly Stack<Condition> _groupStack = new Stack<Condition>();

        // Add a method to extract conditions with proper nesting
        public static ConditionNode ExtractConditions<TModel>(Expression<Func<TModel, bool>> expression)
        {
            try
            {
                // Process the expression recursively
                return ProcessExpression(expression.Body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in expression visitor: {ex.Message}");

                // Return an empty condition node
                return null;
            }
        }

        // Process expression recursively
        private static ConditionNode ProcessExpression(Expression expression)
        {
            if (expression == null) return null;

            // Handle binary expressions (==, !=, >, >=, <, <=, &&, ||)
            if (expression is BinaryExpression binary)
            {
                return ProcessBinaryExpression(binary);
            }

            // Handle method calls (HasValue, Contains, etc.)
            if (expression is MethodCallExpression methodCall)
            {
                return ProcessMethodCallExpression(methodCall);
            }

            // Handle unary expressions (!x.IsActive)
            if (expression is UnaryExpression unary)
            {
                return ProcessUnaryExpression(unary);
            }

            // Handle direct boolean properties (x.IsActive)
            if (expression is MemberExpression member && member.Type == typeof(bool))
            {
                return ProcessBooleanMemberExpression(member);
            }

            System.Diagnostics.Debug.WriteLine($"Unsupported expression type: {expression.NodeType}");
            return null;
        }

        private static ConditionNode ProcessBinaryExpression(BinaryExpression binary)
        {
            // Handle logical operators (AND, OR)
            if (binary.NodeType == ExpressionType.AndAlso)
            {
                var leftNode = ProcessExpression(binary.Left);
                var rightNode = ProcessExpression(binary.Right);

                if (leftNode == null && rightNode == null) return null;

                var andNode = new ConditionNode
                {
                    NodeType = ConditionNodeType.Group,
                    Rule = "AND",
                    Children = new List<ConditionNode>()
                };

                if (leftNode != null)
                {
                    // If left side is already an AND group, merge it
                    if (leftNode.NodeType == ConditionNodeType.Group && leftNode.Rule == "AND")
                    {
                        andNode.Children.AddRange(leftNode.Children);
                    }
                    else
                    {
                        andNode.Children.Add(leftNode);
                    }
                }

                if (rightNode != null)
                {
                    // If right side is already an AND group, merge it
                    if (rightNode.NodeType == ConditionNodeType.Group && rightNode.Rule == "AND")
                    {
                        andNode.Children.AddRange(rightNode.Children);
                    }
                    else
                    {
                        andNode.Children.Add(rightNode);
                    }
                }

                return andNode;
            }
            else if (binary.NodeType == ExpressionType.OrElse)
            {
                var leftNode = ProcessExpression(binary.Left);
                var rightNode = ProcessExpression(binary.Right);

                if (leftNode == null && rightNode == null) return null;

                var orNode = new ConditionNode
                {
                    NodeType = ConditionNodeType.Group,
                    Rule = "OR",
                    Children = new List<ConditionNode>()
                };

                if (leftNode != null)
                {
                    // If left side is already an OR group, merge it
                    if (leftNode.NodeType == ConditionNodeType.Group && leftNode.Rule == "OR")
                    {
                        orNode.Children.AddRange(leftNode.Children);
                    }
                    else
                    {
                        orNode.Children.Add(leftNode);
                    }
                }

                if (rightNode != null)
                {
                    // If right side is already an OR group, merge it
                    if (rightNode.NodeType == ConditionNodeType.Group && rightNode.Rule == "OR")
                    {
                        orNode.Children.AddRange(rightNode.Children);
                    }
                    else
                    {
                        orNode.Children.Add(rightNode);
                    }
                }

                return orNode;
            }

            // Handle comparison operators (==, !=, >, >=, <, <=)
            if (binary.NodeType == ExpressionType.Equal ||
                binary.NodeType == ExpressionType.NotEqual ||
                binary.NodeType == ExpressionType.GreaterThan ||
                binary.NodeType == ExpressionType.GreaterThanOrEqual ||
                binary.NodeType == ExpressionType.LessThan ||
                binary.NodeType == ExpressionType.LessThanOrEqual)
            {
                return CreateComparisonNode(binary);
            }

            return null;
        }

        private static ConditionNode CreateComparisonNode(BinaryExpression binary)
        {
            // Extract property name
            string propertyName = ExtractNestedPath(binary.Left);
            if (propertyName == null) return null;

            // Get operator
            string op = binary.NodeType switch
            {
                ExpressionType.Equal => "==",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                _ => binary.NodeType.ToString()
            };

            // Process right side for value
            bool isProperty = false;
            object value = null;

            // Check if right side is a property
            string rightPropertyName = ExtractNestedPath(binary.Right);
            if (rightPropertyName != null)
            {
                value = rightPropertyName;
                isProperty = true;
            }
            else
            {
                // Try to evaluate the right side
                value = EvaluateExpression(binary.Right);
            }

            // Create condition node
            return new ConditionNode
            {
                NodeType = ConditionNodeType.Condition,
                Property = propertyName,
                Operator = op,
                Value = value,
                IsProperty = isProperty
            };
        }

        private static ConditionNode ProcessMethodCallExpression(MethodCallExpression methodCall)
        {
            // Handle HasValue for nullable types
            if (methodCall.Method.Name == "HasValue" && methodCall.Object is MemberExpression memberExpr)
            {
                string propertyName = memberExpr.Member.Name;

                return new ConditionNode
                {
                    NodeType = ConditionNodeType.Condition,
                    Property = propertyName,
                    Operator = "!=",
                    Value = null,
                    IsProperty = false
                };
            }

            // Handle string methods
            if (methodCall.Object is MemberExpression stringMemberExpr && stringMemberExpr.Type == typeof(string))
            {
                if (methodCall.Method.Name == "Contains" ||
                    methodCall.Method.Name == "StartsWith" ||
                    methodCall.Method.Name == "EndsWith")
                {
                    string propertyName = stringMemberExpr.Member.Name;
                    object value = null;

                    if (methodCall.Arguments.Count > 0)
                    {
                        value = EvaluateExpression(methodCall.Arguments[0]);
                    }

                    if (value != null)
                    {
                        return new ConditionNode
                        {
                            NodeType = ConditionNodeType.Condition,
                            Property = propertyName,
                            Operator = methodCall.Method.Name.ToLowerInvariant(),
                            Value = value,
                            IsProperty = false
                        };
                    }
                }
            }

            return null;
        }

        private static ConditionNode ProcessUnaryExpression(UnaryExpression unary)
        {
            // Handle boolean negation
            if (unary.NodeType == ExpressionType.Not)
            {
                if (unary.Operand is MemberExpression memberExpr &&
                    memberExpr.Type == typeof(bool) &&
                    memberExpr.Expression != null)
                {
                    // For direct property access: !x.Property
                    return new ConditionNode
                    {
                        NodeType = ConditionNodeType.Condition,
                        Property = memberExpr.Member.Name,
                        Operator = "==",
                        Value = false,
                        IsProperty = false
                    };
                }
                else if (unary.Operand is MethodCallExpression methodCall &&
                        methodCall.Method.Name == "HasValue" &&
                        methodCall.Object is MemberExpression hasValueMemberExpr)
                {
                    // For !x.Property.HasValue
                    return new ConditionNode
                    {
                        NodeType = ConditionNodeType.Condition,
                        Property = hasValueMemberExpr.Member.Name,
                        Operator = "==",
                        Value = null,
                        IsProperty = false
                    };
                }

                // For more complex expressions, negate the condition
                var innerNode = ProcessExpression(unary.Operand);
                if (innerNode != null && innerNode.NodeType == ConditionNodeType.Condition)
                {
                    // Negate the condition
                    if (innerNode.Operator == "==") innerNode.Operator = "!=";
                    else if (innerNode.Operator == "!=") innerNode.Operator = "==";
                    else if (innerNode.Operator == ">") innerNode.Operator = "<=";
                    else if (innerNode.Operator == ">=") innerNode.Operator = "<";
                    else if (innerNode.Operator == "<") innerNode.Operator = ">=";
                    else if (innerNode.Operator == "<=") innerNode.Operator = ">";

                    return innerNode;
                }
            }

            return null;
        }

        private static ConditionNode ProcessBooleanMemberExpression(MemberExpression member)
        {
            if (member.Expression != null && member.Expression.NodeType == ExpressionType.Parameter)
            {
                return new ConditionNode
                {
                    NodeType = ConditionNodeType.Condition,
                    Property = member.Member.Name,
                    Operator = "==",
                    Value = true,
                    IsProperty = false
                };
            }

            return null;
        }

        /// <summary>
        /// Helper method to extract nested property paths (static method)
        /// </summary>
        private static string ExtractNestedPath(Expression expr)
        {
            if (expr is MemberExpression memberExpression)
            {
                if (memberExpression.Expression is ParameterExpression)
                {
                    // Root property
                    return memberExpression.Member.Name;
                }
                else if (memberExpression.Expression is MemberExpression innerMember)
                {
                    // Nested property
                    string parentPath = ExtractNestedPath(memberExpression.Expression);
                    if (!string.IsNullOrEmpty(parentPath))
                    {
                        return $"{parentPath}.{memberExpression.Member.Name}";
                    }
                }

                return memberExpression.Member.Name;
            }

            if (expr is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            {
                return ExtractNestedPath(unary.Operand);
            }

            return null;
        }

        private static object EvaluateExpression(Expression expr)
        {
            // Handle constants directly
            if (expr is ConstantExpression constant)
            {
                return constant.Value;
            }

            // Handle member expressions (e.g., fields, properties)
            if (expr is MemberExpression memberExpr)
            {
                // Check if it's a static member or a constant
                if (memberExpr.Expression == null ||
                    memberExpr.Expression is ConstantExpression)
                {
                    try
                    {
                        return Expression.Lambda(memberExpr).Compile().DynamicInvoke();
                    }
                    catch
                    {
                        // If we can't evaluate, return null
                        return null;
                    }
                }
            }

            // Try to evaluate other expressions
            try
            {
                return Expression.Lambda(expr).Compile().DynamicInvoke();
            }
            catch
            {
                // If we can't evaluate, return null
                return null;
            }
        }
    }

    // Define node types for the condition tree
    public enum ConditionNodeType
    {
        Group,      // A group of conditions (AND/OR)
        Condition   // A single condition (property, operator, value)
    }

    // Define a node in the condition tree
    public class ConditionNode
    {
        public ConditionNodeType NodeType { get; set; }

        // For Group nodes
        public string Rule { get; set; }                   // "AND" or "OR"
        public List<ConditionNode> Children { get; set; }  // Child conditions

        // For Condition nodes
        public string Property { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public bool IsProperty { get; set; }

        // Helper methods for building the tree
        public ConditionNode AddChild(ConditionNode child)
        {
            if (NodeType != ConditionNodeType.Group)
            {
                throw new InvalidOperationException("Cannot add child to a condition node");
            }

            if (Children == null)
            {
                Children = new List<ConditionNode>();
            }

            Children.Add(child);
            return this;
        }
    }
}