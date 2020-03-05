import * as React from 'react';
import { FilterTerm, FilterPane, FilterSection } from '@equinor/fusion-components';

type GenericFilterProps<T> = {
    data: T[] | null;
    filterSections: FilterSection<T>[];
    onFilter: (filteredData: T[]) => void;
};

function GenericFilter<T>({ data, filterSections, onFilter }: GenericFilterProps<T>) {
    const [filterTerms, setFilterTerms] = React.useState<FilterTerm[]>([]);

    const onFilterChange = React.useCallback((filteredData: T[], terms: FilterTerm[]) => {
        setFilterTerms(terms);
        onFilter(filteredData);
    }, []);

    React.useEffect(() => {
        if (data) {
            onFilter(data);
        }
    }, [data]);

    return (
        <FilterPane
            data={data}
            sectionDefinitions={filterSections}
            terms={filterTerms}
            onChange={onFilterChange}
        />
    );
}

export default GenericFilter;
