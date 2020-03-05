import { FilterSection, FilterTypes } from '@equinor/fusion-components';
import Contract from '../../../../models/contract';

const getFilterSections = (contracts: Contract[]): FilterSection<Contract>[] => {
    const uniqueCompanies = contracts
        .map(request => request.company?.name || '')
        .filter((d, i, l) => l.indexOf(d) === i);

    return [
        {
            key: 'search-section',
            title: '',
            filters: [
                {
                    key: 'search-filter',
                    type: FilterTypes.Search,
                    title: 'Search',
                    getValue: item =>
                        (item.id || '') +
                        (item.contractNumber || '') +
                        (item.name || '') +
                        (item.company?.name || '') +
                        (item.companyRep?.instances.map(i => i.assignedPerson?.name).join('') ||
                            '') +
                        (item.companyRep?.basePosition.name || '') +
                        (item.contractResponsible?.instances
                            .map(i => i.assignedPerson?.name)
                            .join('') || '') +
                        (item.contractResponsible?.basePosition.name || '') +
                        (item.externalCompanyRep?.instances
                            .map(i => i.assignedPerson?.name)
                            .join('') || '') +
                        (item.externalCompanyRep?.basePosition.name || '') +
                        (item.externalContractResponsible?.instances
                            .map(i => i.assignedPerson?.name)
                            .join('') || '') +
                        (item.externalContractResponsible?.basePosition.name || ''),
                },
            ],
        },
        {
            key: 'filters',
            title: 'Filters',
            isCollapsible: true,
            filters: [
                {
                    key: 'company',
                    title: 'Company',
                    type: FilterTypes.Checkbox,
                    getValue: request => request.company?.name || '',
                    isVisibleWhenPaneIsCollapsed: true,
                    isCollapsible: true,
                    options: uniqueCompanies.map(company => ({
                        key: company,
                        label: company,
                    })),
                },
            ],
        },
    ];
};

export default getFilterSections;
