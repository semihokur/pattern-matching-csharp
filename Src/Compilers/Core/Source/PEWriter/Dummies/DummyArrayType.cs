﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyArrayType : IArrayTypeReference
    {
        #region ITypeDefinition Members

        public ushort Alignment
        {
            get { return 0; }
        }

        public IEnumerable<ITypeReference> BaseClasses
        {
            get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
        }

        public IEnumerable<IEventDefinition> Events
        {
            get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
        }

        public IEnumerable<IFieldDefinition> Fields
        {
            get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
        }

        public IEnumerable<IMethodDefinition> Methods
        {
            get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
        }

        public IEnumerable<INestedTypeDefinition> NestedTypes
        {
            get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
        }

        public IEnumerable<IPropertyDefinition> Properties
        {
            get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
        }

        public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides
        {
            get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
        }

        public IEnumerable<IGenericTypeParameter> GenericParameters
        {
            get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
        }

        public ushort GenericParameterCount
        {
            get
            {
                // ^ assume false;
                return 0;
            }
        }

        public IGenericTypeInstanceReference InstanceType
        {
            get { return Dummy.GenericTypeInstance; }
        }

        public IEnumerable<ITypeReference> Interfaces
        {
            get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
        }

        public bool IsAbstract
        {
            get { return false; }
        }

        public bool IsClass
        {
            get { return true; }
        }

        public bool IsDelegate
        {
            get { return false; }
        }

        public bool IsEnum
        {
            get { return false; }
        }

        public bool IsGeneric
        {
            get { return false; }
        }

        public bool IsInterface
        {
            get { return false; }
        }

        public bool IsReferenceType
        {
            get { return false; }
        }

        public bool IsSealed
        {
            get { return true; }
        }

        public bool IsStatic
        {
            get { return true; }
        }

        public bool IsValueType
        {
            get { return false; }
        }

        public bool IsStruct
        {
            get { return false; }
        }

        public IEnumerable<ITypeDefinitionMember> Members
        {
            get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
        }

        public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers
        {
            get { return this.Members; }
        }

        public uint SizeOf
        {
            get { return 0; }
        }

        public IEnumerable<ISecurityAttribute> SecurityAttributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
        }

        public ITypeReference UnderlyingType
        {
            get { return Dummy.TypeReference; }
        }

        public PrimitiveTypeCode TypeCode
        {
            get { return PrimitiveTypeCode.Invalid; }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public LayoutKind Layout
        {
            get { return LayoutKind.Auto; }
        }

        public bool IsSpecialName
        {
            get { return false; }
        }

        public bool IsComObject
        {
            get { return false; }
        }

        public bool IsSerializable
        {
            get { return false; }
        }

        public bool IsBeforeFieldInit
        {
            get { return false; }
        }

        public StringFormatKind StringFormat
        {
            get { return StringFormatKind.Ansi; }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        #endregion

        #region IDefinition Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion

        #region IScope<ITypeDefinitionMember> Members

        public bool Contains(ITypeDefinitionMember member)
        {
            return false;
        }

        public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate)
        {
            return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
        }

        public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate)
        {
            return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
        }

        public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase)
        {
            return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
        }

        #endregion

        #region ITypeReference Members

        public bool IsAlias
        {
            get { return false; }
        }

        public IAliasForType AliasForType
        {
            get { return Dummy.AliasForType; }
        }

        public ITypeDefinition ResolvedType
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public uint TypeDefRowId
        {
            get { return 0; }
        }

        #endregion

        #region IArrayTypeReference Members

        public ITypeReference ElementType
        {
            get { return Dummy.TypeReference; }
        }

        public bool IsVector
        {
            get { return true; }
        }

        public IEnumerable<int> LowerBounds
        {
            get { return IteratorHelper.GetEmptyEnumerable<int>(); }
        }

        public uint Rank
        {
            get { return 0; }
        }

        public IEnumerable<ulong> Sizes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ulong>(); }
        }

        #endregion

        IEnumerable<ICustomAttribute> IReference.Attributes
        {
            get { throw new NotImplementedException(); }
        }

        void IReference.Dispatch(IMetadataVisitor visitor)
        {
            throw new NotImplementedException();
        }

        IEnumerable<ILocation> IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeReference.IsEnum
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeReference.IsValueType
        {
            get { throw new NotImplementedException(); }
        }

        ITypeDefinition ITypeReference.ResolvedType
        {
            get { throw new NotImplementedException(); }
        }

        PrimitiveTypeCode ITypeReference.TypeCode
        {
            get { throw new NotImplementedException(); }
        }

        ITypeReference IArrayTypeReference.ElementType
        {
            get { throw new NotImplementedException(); }
        }

        bool IArrayTypeReference.IsVector
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<int> IArrayTypeReference.LowerBounds
        {
            get { throw new NotImplementedException(); }
        }

        uint IArrayTypeReference.Rank
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ulong> IArrayTypeReference.Sizes
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsNamespaceTypeReference
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsGenericTypeInstance
        {
            get { throw new NotImplementedException(); }
        }
    }
}