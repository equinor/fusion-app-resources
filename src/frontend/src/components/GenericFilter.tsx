import { useState, useCallback, useEffect } from 'react';
import { FilterTerm, FilterPane, FilterSection } from '@equinor/fusion-components';

type GenericFilterProps<T> = {
    data: T[] | null;
    filterSections: FilterSection<T>[];
    onFilter: (filteredData: T[]) => void;
    id?: string;
};

function GenericFilter<T>({ data, filterSections, onFilter, id }: GenericFilterProps<T>) {
    const [filterTerms, setFilterTerms] = useState<FilterTerm[]>([]);

    const onFilterChange = useCallback((filteredData: T[], terms: FilterTerm[]) => {
        setFilterTerms(terms);
        onFilter(filteredData);
    }, []);

    useEffect(() => {
        if (data) {
            onFilter(data);
        }
    }, [data]);

    return (
        <FilterPane
            id="filter-pane"
            data={data}
            sectionDefinitions={filterSections}
            terms={filterTerms}
            onChange={onFilterChange}
        />
    );
}

export default GenericFilter;
