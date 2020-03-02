import * as React from 'react';
import { SearchableDropdown, SearchableDropdownOption } from '@equinor/fusion-components';
import Company from '../../../../../models/company';

type CompanyPickerProps = {
    selectedCompanyId: string | null;
    onSelect: (company: Company) => void;
};

const companies: Company[] = [
    {
        id: '08eebd4f-697b-4ef2-8119-00eaac87531c',
        identifier: '123',
        name: 'Company 123',
    },
];

const CompanyPicker: React.FC<CompanyPickerProps> = ({ selectedCompanyId, onSelect }) => {
    const options = React.useMemo(() => {
        return companies.map(company => ({
            title: company.name || company.id,
            key: company.id,
            isSelected: company.id === selectedCompanyId,
        }));
    }, [companies, selectedCompanyId]);

    const onDropdownSelect = React.useCallback(
        (option: SearchableDropdownOption) => {
            const selectedCompany = companies.find(company => company.id === option.key);
            if (selectedCompany) {
                onSelect(selectedCompany);
            }
        },
        [onSelect]
    );

    return <SearchableDropdown label="Company" options={options} onSelect={onDropdownSelect} />;
};

export default CompanyPicker;
