import * as React from 'react';
import Personnel, { PersonnelDiscipline } from '../../../../../../../../models/Personnel';
import * as styles from './styles.less'
import { PersonPhoto, TextInput, SearchableDropdownOption, SearchableDropdown } from '@equinor/fusion-components';
import { PersonDetails } from '@equinor/fusion';
import AzureAdStatusIcon from '../../components/AzureAdStatus';
import classNames from 'classnames';
import useBasePositions from '../../../../../../../../hooks/useBasePositions';
import AddPersonnelFormDisciplinesDropDown from '../../AddPersonnelSideSheet/AddPersonnelFormDisciplinesDropDown';

type GeneralTabProps = {
  person: Personnel;
  edit?: boolean;
};

const createTextField = (
  label: string,
  text: string | React.ReactNode,
  textStyle?: string | null,
) => {
  return (
    <div className={styles.field}>
      <label>{label}</label>
      <div className={classNames(styles.value, textStyle ? styles[textStyle] : null)} >
        {text}
      </div>
    </div>
  );
};

const createEditField = (
  label: string,
  value: string,
  onChange: (newValue: string) => void,
) => {

  return (
    <div className={styles.field}>
      <label>{label}</label>
      <TextInput
        key={label}
        onChange={(newValue) => onChange(newValue)}
        value={value}
      />
    </div>
  )
}

const GeneralTab: React.FC<GeneralTabProps> = ({
  person,
  edit
}) => {

  const { basePositions, isFetchingBasePositions } = useBasePositions();

  const [firstName, setFirstName] = React.useState<string>('');
  const [lastName, setLastName] = React.useState<string>('');
  const [phoneNumber, setPhoneNumber] = React.useState<string>('');
  const [disciplines, setDisciplines] = React.useState<PersonnelDiscipline[]>([]);

  React.useEffect(() => {
    if (!edit) return

    setFirstName(person.firstName || '');
    setLastName(person.lastName || '');
    setPhoneNumber(person.phoneNumber || '');
    setDisciplines(person.disciplines);

  }, [edit])

  const personnelDetails: PersonDetails = {
    azureUniqueId: person.azureUniquePersonId || '',
    name: person.name,
    mail: person.mail,
    jobTitle: person.jobTitle,
    department: null,
    mobilePhone: person.phoneNumber,
    officeLocation: null,
    upn: '',
    accountType: 'External',
    company: { id: '', name: '' },
  }

  const dropDownOptions = React.useMemo(() => {
    const disciplineOptions: SearchableDropdownOption[] = [];
    return basePositions.reduce((d, b): SearchableDropdownOption[] => {
      if (d.some(d => d.key === b.discipline) || !b.discipline.length) return d;

      d.push({
        title: b.discipline,
        key: b.discipline,
        isSelected: b.discipline === disciplines[0]?.name,
      });

      return d;
    }, disciplineOptions);
  }, [basePositions, disciplines]);

  const onSelect = React.useCallback(
    (newValue: SearchableDropdownOption) => {
      setDisciplines([{ name: newValue.title }])
    }, []);


  return (
    <div className={styles.container}>
      <div className={styles.personnelDetails}>
        <div className={styles.personPhoto}>
          <PersonPhoto hideTooltip={true} person={personnelDetails} size={'xlarge'} />
        </div>
        <div>
          {edit
            ? (<>
              <div className={styles.row}>
                {createEditField('First name', firstName, setFirstName)}
                {createEditField('Last name', lastName, setLastName)}
              </div>
              <div className={styles.row}>
                {createEditField('Phone', phoneNumber, setPhoneNumber)}
                {createTextField('E-Mail', person.mail || '')}
                {createTextField('AD Status', AzureAdStatusIcon(person.azureAdStatus || 'NoAccount'), 'AdStatus')}
              </div>
            </>
            )
            : (<>
              <div className={styles.row}>
                {createTextField('First name', person.firstName || '')}
                {createTextField('Last name', person.lastName || '')}
              </div>
              <div className={styles.row}>
                {createTextField('Phone', person.phoneNumber || '')}
                {createTextField('E-Mail', person.mail || '')}
                {createTextField('AD Status', AzureAdStatusIcon(person.azureAdStatus || 'NoAccount'), 'AdStatus')}
              </div>  </>
            )
          }

        </div>
      </div>
      <div className={styles.disciplines}>
        <h4>Disciplines</h4>
        {edit ? <div className={styles.disciplineDropDown}>
          <SearchableDropdown
            options={dropDownOptions}
            onSelect={onSelect}
          /></div> :
          <div className={styles.disciplinesChips}>
            {person.disciplines.map(d => <div key={d.name} className={styles.disciplineChip}>{d.name}</div>)}
          </div>
        }

      </div>
    </div>
  );
};

export default GeneralTab;
