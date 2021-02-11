
import { SearchableDropdown, SearchableDropdownOption } from '@equinor/fusion-components';
import { FC, useMemo, useCallback } from 'react';
import Company from '../../../../../models/company';
import useCompanies from '../hooks/useCompanies';

type CompanyPickerProps = {
    selectedCompanyId: string | null;
    onSelect: (company: Company) => void;
};

const CompanyPicker: FC<CompanyPickerProps> = ({ selectedCompanyId, onSelect }) => {
    const { companies } = useCompanies();

    const options = useMemo(() => {
        return companies.map(company => ({
            title: company.name || company.id,
            key: company.id,
            isSelected: company.id === selectedCompanyId,
        }));
    }, [companies, selectedCompanyId]);

    const onDropdownSelect = useCallback(
        (option: SearchableDropdownOption) => {
            const selectedCompany = companies.find(company => company.id === option.key);
            if (selectedCompany) {
                onSelect(selectedCompany);
            }
        },
        [onSelect, companies]
    );

    return <SearchableDropdown label="Company" options={options} onSelect={onDropdownSelect} />;
};

export default CompanyPicker;
