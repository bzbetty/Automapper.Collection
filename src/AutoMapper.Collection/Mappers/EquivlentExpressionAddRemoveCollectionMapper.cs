﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper.Configuration;
using AutoMapper.EquivilencyExpression;

namespace AutoMapper.Mappers
{
    public class EquivlentExpressionAddRemoveCollectionMapper : IObjectMapper
    {
        private readonly CollectionMapper CollectionMapper = new CollectionMapper();
        public static TDestination Map<TSource, TSourceItem, TDestination, TDestinationItem>(TSource source, TDestination destination, ResolutionContext context, Func<TDestination> ifNullFunc)
            where TSource : IEnumerable<TSourceItem>
            where TDestination : class, ICollection<TDestinationItem>
        {
            if (source == null || destination == null)
                return ifNullFunc();

            var equivilencyExpression = GetEquivilentExpression(new TypePair(typeof(TSource), typeof(TDestination))) as IEquivilentExpression<TSourceItem,TDestinationItem>;
            var compareSourceToDestination = source.ToDictionary(s => s, s => destination.FirstOrDefault(d => equivilencyExpression.IsEquivlent(s, d)));

            foreach (var removedItem in destination.Except(compareSourceToDestination.Values).ToList())
                destination.Remove(removedItem);

            foreach (var keypair in compareSourceToDestination)
            {
                if (keypair.Value == null)
                    destination.Add(context.Mapper.Map<TDestinationItem>(keypair.Key));
                else
                    context.Mapper.Map(keypair.Key, keypair.Value);
            }

            return destination;
        }

        private static readonly MethodInfo MapMethodInfo = typeof(EquivlentExpressionAddRemoveCollectionMapper).GetRuntimeMethods().First(_ => _.IsStatic);
        

        public bool IsMatch(TypePair typePair)
        {
            return typePair.SourceType.IsEnumerableType()
                   && typePair.DestinationType.IsCollectionType()
                   && GetEquivilentExpression(typePair) != null;
        }

        public Expression MapExpression(TypeMapRegistry typeMapRegistry, IConfigurationProvider configurationProvider,
            PropertyMap propertyMap, Expression sourceExpression, Expression destExpression, Expression contextExpression)
        {
            var collectionExpression = CollectionMapper.MapExpression(typeMapRegistry, configurationProvider, propertyMap, sourceExpression, destExpression, contextExpression);
            var collectionFunc = Expression.Lambda(collectionExpression).Compile();
            return Expression.Call(null,
                MapMethodInfo.MakeGenericMethod(sourceExpression.Type, TypeHelper.GetElementType(sourceExpression.Type), destExpression.Type, TypeHelper.GetElementType(destExpression.Type)),
                    sourceExpression, destExpression, contextExpression, Expression.Constant(collectionFunc));
        }

        private static IEquivilentExpression GetEquivilentExpression(TypePair typePair)
        {
            return EquivilentExpressions.GetEquivilentExpression(TypeHelper.GetElementType(typePair.SourceType), TypeHelper.GetElementType(typePair.DestinationType));
        }
    }
}
