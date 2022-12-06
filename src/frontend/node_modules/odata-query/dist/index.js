'use strict';

Object.defineProperty(exports, "__esModule", {
  value: true
});

var _slicedToArray = function () { function sliceIterator(arr, i) { var _arr = []; var _n = true; var _d = false; var _e = undefined; try { for (var _i = arr[Symbol.iterator](), _s; !(_n = (_s = _i.next()).done); _n = true) { _arr.push(_s.value); if (i && _arr.length === i) break; } } catch (err) { _d = true; _e = err; } finally { try { if (!_n && _i["return"]) _i["return"](); } finally { if (_d) throw _e; } } return _arr; } return function (arr, i) { if (Array.isArray(arr)) { return arr; } else if (Symbol.iterator in Object(arr)) { return sliceIterator(arr, i); } else { throw new TypeError("Invalid attempt to destructure non-iterable instance"); } }; }();

var _typeof = typeof Symbol === "function" && typeof Symbol.iterator === "symbol" ? function (obj) { return typeof obj; } : function (obj) { return obj && typeof Symbol === "function" && obj.constructor === Symbol && obj !== Symbol.prototype ? "symbol" : typeof obj; };

exports.default = function () {
  var _ref = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {},
      select = _ref.select,
      filter = _ref.filter,
      search = _ref.search,
      groupBy = _ref.groupBy,
      transform = _ref.transform,
      orderBy = _ref.orderBy,
      top = _ref.top,
      skip = _ref.skip,
      key = _ref.key,
      count = _ref.count,
      expand = _ref.expand,
      action = _ref.action,
      func = _ref.func,
      format = _ref.format;

  var path = '';
  var params = {};

  if (select) {
    params.$select = select;
  }

  if (filter || count instanceof Object) {
    var builtFilter = buildFilter(count instanceof Object ? count : filter);
    if (builtFilter !== undefined) {
      params.$filter = builtFilter;
    }
  }

  if (search) {
    params.$search = search;
  }

  if (transform) {
    var builtTransforms = buildTransforms(transform);
    if (builtTransforms !== undefined) {
      params.$apply = builtTransforms;
    }
  }

  if (top) {
    params.$top = top;
  }

  if (skip) {
    params.$skip = skip;
  }

  if (key) {
    if ((typeof key === 'undefined' ? 'undefined' : _typeof(key)) === 'object') {
      var keys = Object.keys(key).map(function (k) {
        return k + '=' + key[k];
      }).join(',');
      path += '(' + keys + ')';
    } else {
      path += '(' + key + ')';
    }
  }

  if (count) {
    if (typeof count === 'boolean') {
      params.$count = true;
    } else {
      path += '/$count';
    }
  }

  if (action) {
    path += '/' + action;
  }

  if (func) {
    if (typeof func === 'string') {
      path += '/' + func;
    } else if ((typeof func === 'undefined' ? 'undefined' : _typeof(func)) === 'object') {
      var _Object$keys = Object.keys(func),
          _Object$keys2 = _slicedToArray(_Object$keys, 1),
          funcName = _Object$keys2[0];

      var funcArgs = Object.keys(func[funcName]).reduce(function (acc, item) {
        var value = func[funcName][item];
        if (Array.isArray(value) && _typeof(value[0]) === 'object') {
          acc.params.push(item + '=@' + item);
          acc.aliases.push('@' + item + '=' + escape(JSON.stringify(value)));
        } else {
          acc.params.push(item + '=' + handleValue(value));
        }
        return acc;
      }, { params: [], aliases: [] });

      path += '/' + funcName;
      if (funcArgs.params.length) {
        path += '(' + funcArgs.params.join(',') + ')';
      }
      if (funcArgs.aliases.length) {
        path += '?' + funcArgs.aliases.join(',');
      }
    }
  }

  if (expand) {
    params.$expand = buildExpand(expand);
  }

  if (orderBy) {
    params.$orderby = buildOrderBy(orderBy);
  }

  if (format) {
    params.$format = format;
  }

  return buildUrl(path, params);
};

function _defineProperty(obj, key, value) { if (key in obj) { Object.defineProperty(obj, key, { value: value, enumerable: true, configurable: true, writable: true }); } else { obj[key] = value; } return obj; }

var COMPARISON_OPERATORS = ['eq', 'ne', 'gt', 'ge', 'lt', 'le'];
var LOGICAL_OPERATORS = ['and', 'or', 'not'];
var COLLECTION_OPERATORS = ['any', 'all'];
var BOOLEAN_FUNCTIONS = ['startswith', 'endswith', 'contains'];
var SUPPORTED_EXPAND_PROPERTIES = ['expand', 'select', 'top', 'orderby', 'filter'];

var FUNCTION_REGEX = /\((.*)\)/;
var INDEXOF_REGEX = /(?!indexof)\((\w+)\)/;

function buildFilter() {
  var filters = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
  var propPrefix = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : '';

  if (filters == null) {
    // ignore `null` and `undefined` filters (useful for conditionally applied filters)
    return;
  } else if (typeof filters === 'string') {
    // Use raw filter string
    return filters;
  } else if (Array.isArray(filters)) {
    var builtFilters = filters.map(function (f) {
      return buildFilter(f, propPrefix);
    }).filter(function (f) {
      return f !== undefined;
    });
    if (builtFilters.length) {
      return '' + builtFilters.map(function (f) {
        return '(' + f + ')';
      }).join(' and ');
    }
  } else if ((typeof filters === 'undefined' ? 'undefined' : _typeof(filters)) === 'object') {
    var filtersArray = Object.keys(filters).reduce(function (result, filterKey) {
      var value = filters[filterKey];
      var propName = '';
      if (propPrefix) {
        if (INDEXOF_REGEX.test(filterKey)) {
          propName = filterKey.replace(INDEXOF_REGEX, '(' + propPrefix + '/$1)');
        } else if (FUNCTION_REGEX.test(filterKey)) {
          propName = filterKey.replace(FUNCTION_REGEX, '(' + propPrefix + '/$1)');
        } else {
          propName = propPrefix + '/' + filterKey;
        }
      } else {
        propName = filterKey;
      }

      if (['number', 'string', 'boolean'].indexOf(typeof value === 'undefined' ? 'undefined' : _typeof(value)) !== -1 || value instanceof Date || value === null) {
        // Simple key/value handled as equals operator
        result.push(propName + ' eq ' + handleValue(value));
      } else if (Array.isArray(value)) {
        var op = filterKey;
        var _builtFilters = value.map(function (v) {
          return buildFilter(v, propPrefix);
        }).filter(function (f) {
          return f !== undefined;
        }).map(function (f) {
          return LOGICAL_OPERATORS.indexOf(op) !== -1 ? '(' + f + ')' : f;
        });
        if (_builtFilters.length) {
          if (LOGICAL_OPERATORS.indexOf(op) !== -1) {
            if (_builtFilters.length) {
              if (op === 'not') {
                result.push(parseNot(op, _builtFilters));
              } else {
                result.push('(' + _builtFilters.join(' ' + op + ' ') + ')');
              }
            }
          } else {
            result.push(_builtFilters.join(' ' + op + ' '));
          }
        }
      } else if (LOGICAL_OPERATORS.indexOf(propName) !== -1) {
        var _op = propName;
        var _builtFilters2 = Object.keys(value).map(function (valueKey) {
          return buildFilter(_defineProperty({}, valueKey, value[valueKey]));
        });
        if (_builtFilters2.length) {
          if (_op === 'not') {
            result.push(parseNot(_op, _builtFilters2));
          } else {
            result.push('' + _builtFilters2.join(' ' + _op + ' '));
          }
        }
      } else if (value instanceof Object) {
        if ('type' in value) {
          result.push(propName + ' eq ' + handleValue(value));
        } else {
          var operators = Object.keys(value);
          operators.forEach(function (op) {
            if (COMPARISON_OPERATORS.indexOf(op) !== -1) {
              result.push(propName + ' ' + op + ' ' + handleValue(value[op]));
            } else if (LOGICAL_OPERATORS.indexOf(op) !== -1) {
              if (Array.isArray(value[op])) {
                result.push(value[op].map(function (v) {
                  return '(' + buildFilter(v, propName) + ')';
                }).join(' ' + op + ' '));
              } else {
                result.push('(' + buildFilter(value[op], propName) + ')');
              }
            } else if (COLLECTION_OPERATORS.indexOf(op) !== -1) {
              var lambaParameter = filterKey.toLowerCase();
              var filter = buildFilter(value[op], lambaParameter);

              if (filter !== undefined) {
                // Do not apply collection filter if undefined (ex. ignore `Foo: { any: {} }`)
                result.push(propName + '/' + op + '(' + lambaParameter + ':' + filter + ')');
              }
            } else if (op === 'in') {
              var resultingValues = Array.isArray(value[op]) ? // Convert `{ Prop: { in: [1,2,3] } }` to `(Prop eq 1 or Prop eq 2 or Prop eq 3)`
              value[op] : // Convert `{ Prop: { in: [{type: type, value: 1},{type: type, value: 2},{type: type, value: 3}] } }`
              // to `(Prop eq 1 or Prop eq 2 or Prop eq 3)`
              value[op].value.map(function (typedValue) {
                return {
                  type: value[op].type,
                  value: typedValue
                };
              });

              result.push('(' + resultingValues.map(function (v) {
                return propName + ' eq ' + handleValue(v);
              }).join(' or ') + ')');
            } else if (BOOLEAN_FUNCTIONS.indexOf(op) !== -1) {
              // Simple boolean functions (startswith, endswith, contains)
              result.push(op + '(' + propName + ',' + handleValue(value[op]) + ')');
            } else {
              // Nested property
              result.push(buildFilter(value, propName));
            }
          });
        }
      } else if (value === undefined) {
        // Ignore/omit filter if value is `undefined`
      } else {
        throw new Error('Unexpected value type: ' + value);
      }

      return result;
    }, []);

    return filtersArray.join(' and ') || undefined;
  } else {
    throw new Error('Unexpected filters type: ' + filters);
  }
}

function escapeIllegalChars(string) {
  string = string.replace(/%/g, '%25');
  string = string.replace(/\+/g, '%2B');
  string = string.replace(/\//g, '%2F');
  string = string.replace(/\?/g, '%3F');
  string = string.replace(/#/g, '%23');
  string = string.replace(/&/g, '%26');
  string = string.replace(/'/g, "''");
  return string;
}

function handleValue(value) {
  if (typeof value === 'string') {
    return '\'' + escapeIllegalChars(value) + '\'';
  } else if (value instanceof Date) {
    return value.toISOString();
  } else if (value instanceof Number) {
    return value;
  } else if (Array.isArray(value)) {
    // Double quote strings to keep them after `.join`
    var arr = value.map(function (d) {
      return typeof d === 'string' ? '\'' + d + '\'' : d;
    });
    return '[' + arr.join(',') + ']';
  } else {
    // TODO: Figure out how best to specify types.  See: https://github.com/devnixs/ODataAngularResources/blob/master/src/odatavalue.js
    switch (value && value.type) {
      case 'guid':
        return value.value;
      case 'raw':
        return value.value;
      case 'binary':
        return 'binary\'' + value.value + '\'';
    }
    return value;
  }
}

function buildExpand(expands) {
  if (typeof expands === 'number') {
    return expands;
  } else if (typeof expands === 'string') {
    if (expands.indexOf('/') === -1) {
      return expands;
    }

    // Change `Foo/Bar/Baz` to `Foo($expand=Bar($expand=Baz))`
    return expands.split('/').reverse().reduce(function (results, item, index, arr) {
      if (index === 0) {
        // Inner-most item
        return '$expand=' + item;
      } else if (index === arr.length - 1) {
        // Outer-most item, don't add `$expand=` prefix (added above)
        return item + '(' + results + ')';
      } else {
        // Other items
        return '$expand=' + item + '(' + results + ')';
      }
    }, '');
  } else if (Array.isArray(expands)) {
    return '' + expands.map(function (e) {
      return buildExpand(e);
    }).join(',');
  } else if ((typeof expands === 'undefined' ? 'undefined' : _typeof(expands)) === 'object') {
    var expandKeys = Object.keys(expands);

    if (expandKeys.some(function (key) {
      return SUPPORTED_EXPAND_PROPERTIES.indexOf(key.toLowerCase()) !== -1;
    })) {
      return expandKeys.map(function (key) {
        var value = key === 'filter' ? buildFilter(expands[key]) : key.toLowerCase() === 'orderby' ? buildOrderBy(expands[key]) : buildExpand(expands[key]);
        return '$' + key.toLowerCase() + '=' + value;
      }).join(';');
    } else {
      return expandKeys.map(function (key) {
        var builtExpand = buildExpand(expands[key]);
        return builtExpand ? key + '(' + builtExpand + ')' : key;
      }).join(',');
    }
  }
}

function buildTransforms(transforms) {
  // Wrap single object an array for simplified processing
  var transformsArray = Array.isArray(transforms) ? transforms : [transforms];

  var transformsResult = transformsArray.reduce(function (result, transform) {
    Object.keys(transform).forEach(function (transformKey) {
      var transformValue = transform[transformKey];
      switch (transformKey) {
        case 'aggregate':
          result.push('aggregate(' + buildAggregate(transformValue) + ')');
          break;
        case 'filter':
          var builtFilter = buildFilter(transformValue);
          if (builtFilter !== undefined) {
            result.push('filter(' + buildFilter(transformValue) + ')');
          }
          break;
        case 'groupby': // support both cases
        case 'groupBy':
          result.push('groupby(' + buildGroupBy(transformValue) + ')');
          break;
        default:
          // TODO: support as many of the following:
          //   topcount, topsum, toppercent,
          //   bottomsum, bottomcount, bottompercent,
          //   identity, concat, expand, search, compute, isdefined
          throw new Error('Unsupported transform: \'' + transformKey + '\'');
      }
    });

    return result;
  }, []);

  return transformsResult.join('/') || undefined;
}

function buildAggregate(aggregate) {
  // Wrap single object in an array for simplified processing
  var aggregateArray = Array.isArray(aggregate) ? aggregate : [aggregate];

  return aggregateArray.map(function (aggregateItem) {
    return Object.keys(aggregateItem).map(function (aggregateKey) {
      var aggregateValue = aggregateItem[aggregateKey];

      // TODO: Are these always required?  Can/should we default them if so?
      if (aggregateValue.with === undefined) {
        throw new Error('\'with\' property required for \'' + aggregateKey + '\'');
      }
      if (aggregateValue.as === undefined) {
        throw new Error('\'as\' property required for \'' + aggregateKey + '\'');
      }

      return aggregateKey + ' with ' + aggregateValue.with + ' as ' + aggregateValue.as;
    });
  }).join(',');
}

function buildGroupBy(groupBy) {
  if (groupBy.properties === undefined) {
    throw new Error('\'properties\' property required for groupBy:\'' + aggregateKey + '\'');
  }

  var result = '(' + groupBy.properties.join(',') + ')';

  if (groupBy.transform) {
    result += ',' + buildTransforms(groupBy.transform);
  }

  return result;
}

function buildOrderBy(orderBy) {
  if (typeof orderBy === 'number') {
    return orderBy;
  } else if (typeof orderBy === 'string') {
    return orderBy;
  } else if (Array.isArray(orderBy)) {
    return '' + orderBy.map(function (o) {
      return buildOrderBy(o);
    }).join(',');
  }
}

function buildUrl(path, params) {
  if (Object.keys(params).length) {
    return path + '?' + Object.keys(params).map(function (key) {
      return key + '=' + params[key];
    }).join('&');
  } else {
    return path;
  }
}

function parseNot(op, builtFilters) {
  if (builtFilters.length > 1) {
    return 'not( ' + builtFilters.join(' and ') + ')';
  } else {
    return builtFilters.map(function (filter) {
      if (filter.charAt(0) === '(') {
        return '(not '.concat(filter.substr(1));
      } else {
        return 'not '.concat(filter);
      }
    });
  }
}