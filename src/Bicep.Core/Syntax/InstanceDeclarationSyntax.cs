// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.Navigation;
using Bicep.Core.Parsing;
using Bicep.Core.TypeSystem;
using Bicep.Core.TypeSystem.Applications;

namespace Bicep.Core.Syntax
{
    public class InstanceDeclarationSyntax : SyntaxBase, INamedDeclarationSyntax
    {
        public InstanceDeclarationSyntax(Token keyword, IdentifierSyntax name, SyntaxBase type, SyntaxBase assignment, SyntaxBase? ifCondition, SyntaxBase body)
        {
            AssertKeyword(keyword, nameof(keyword), LanguageConstants.InstanceKeyword);
            AssertSyntaxType(name, nameof(name), typeof(IdentifierSyntax));
            AssertSyntaxType(type, nameof(type), typeof(StringSyntax), typeof(SkippedTriviaSyntax));
            AssertTokenType(keyword, nameof(keyword), TokenType.Identifier);
            AssertSyntaxType(assignment, nameof(assignment), typeof(Token), typeof(SkippedTriviaSyntax));
            AssertTokenType(assignment as Token, nameof(assignment), TokenType.Assignment);
            AssertSyntaxType(ifCondition, nameof(ifCondition), typeof(SkippedTriviaSyntax), typeof(IfConditionSyntax));
            AssertSyntaxType(body, nameof(body), typeof(SkippedTriviaSyntax), typeof(ObjectSyntax));

            this.Keyword = keyword;
            this.Name = name;
            this.Type = type;
            this.Assignment = assignment;
            this.IfCondition = ifCondition;
            this.Body = body;
        }

        public Token Keyword { get; }

        public IdentifierSyntax Name { get; }

        public SyntaxBase Type { get; }

        public SyntaxBase Assignment { get; }

        public SyntaxBase? IfCondition { get; }

        public SyntaxBase Body { get; }

        public override void Accept(ISyntaxVisitor visitor) => visitor.VisitInstanceDeclarationSyntax(this);

        public override TextSpan Span => TextSpan.Between(Keyword, Body);

        public StringSyntax? TypeString => Type as StringSyntax;

        public TypeSymbol GetDeclaredType(IComponentTypeProvider componentTypeProvider)
        {
            var stringSyntax = this.TypeString;

            if (stringSyntax != null && stringSyntax.IsInterpolated())
            {
                // TODO: in the future, we can relax this check to allow interpolation with compile-time constants.
                // right now, codegen will still generate a format string however, which will cause problems for the type.
                return ErrorType.Create(DiagnosticBuilder.ForPosition(this.Type).ResourceTypeInterpolationUnsupported());
            }

            var stringContent = stringSyntax?.TryGetLiteralValue();
            if (stringContent == null)
            {
                return ErrorType.Create(DiagnosticBuilder.ForPosition(this.Type).InvalidResourceType());
            }

            var typeReference = ComponentTypeReference.TryParse(stringContent);
            if (typeReference == null)
            {
                return ErrorType.Create(DiagnosticBuilder.ForPosition(this.Type).InvalidResourceType());
            }

            return componentTypeProvider.GetInstanceType(typeReference);
        }
    }
}