import * as React from 'react';
import Personnel, { PersonnelDiscipline } from '../../../../../../../../models/Personnel';
import * as styles from './styles.less'
import { PersonPhoto, TextInput, SearchableDropdownOption, SearchableDropdown, Button } from '@equinor/fusion-components';
import { PersonDetails, useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import AzureAdStatusIcon from '../../components/AzureAdStatus';
import classNames from 'classnames';
import useBasePositions from '../../../../../../../../hooks/useBasePositions';
import { useContractContext } from '../../../../../../../../contractContex';
import { useAppContext } from '../../../../../../../../appContext';

type GeneralTabProps = {
  person: Personnel;
  edit: boolean;
  setEdit: (state: boolean) => void;
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
  edit,
  setEdit
}) => {

  const { apiClient } = useAppContext();
  const currentContext = useCurrentContext();
  const { contract, dispatchContractAction } = useContractContext();
  const notification = useNotificationCenter();

  const [firstName, setFirstName] = React.useState<string>('');
  const [lastName, setLastName] = React.useState<string>('');
  const [phoneNumber, setPhoneNumber] = React.useState<string>('');
  const [disciplines, setDisciplines] = React.useState<PersonnelDiscipline[]>([]);

  const { basePositions } = useBasePositions();


  const isDirty = () => {
    return (firstName !== person.firstName ||
      lastName !== person.lastName ||
      phoneNumber !== person.phoneNumber ||
      disciplines !== person.disciplines)
  }

  const savePersonChangesAsync = async () => {
    const contractId = contract?.id;
    if (!currentContext?.id || !contractId) return;

    if (!isDirty()) {
      return setEdit(false)
    }

    try {

      const updatedPerson = {
        ...person,
        firstName: firstName,
        lastName: lastName,
        phoneNumber: phoneNumber,
        disciplines: disciplines
      }

      const response = await apiClient.updatePersonnelAsync(
        currentContext.id,
        contractId,
        updatedPerson
      )

      notification({
        level: 'low',
        title: 'Personnel changes saved',
        cancelLabel: 'dismiss',
      });

      dispatchContractAction({ verb: 'merge', collection: 'personnel', payload: [response] });
      setEdit(false);

    } catch (e) {
      //TODO: This could probably be more helpfull.
      notification({
        level: 'high',
        title:
          'Something went wrong while saving. Please try again or contact administrator',
      });
    }
  };




  React.useEffect(() => {
    if (!edit) return

    setFirstName(person.firstName || '');
    setLastName(person.lastName || '');
    setPhoneNumber(person.phoneNumber || '');
    setDisciplines(person.disciplines);

  }, [edit, person])

  const PersonDetails = React.useMemo<PersonDetails>(() => {
    return {
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
  }, [person])

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
          <PersonPhoto hideTooltip={true} person={PersonDetails} size={'xlarge'} />
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
