import * as React from 'react';
import Personnel from '../../../../../../../../models/Personnel';
import * as styles from './styles.less'
import { PersonPhoto } from '@equinor/fusion-components';
import { PersonDetails } from '@equinor/fusion';
import AzureAdStatusIcon from '../../components/AzureAdStatus';
import classNames from 'classnames';

type GeneralTabProps = {
  person: Personnel;
};


const createField = (
  label: string,
  text: string | React.ReactNode,
  textStyle?: string
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


const GeneralTab: React.FC<GeneralTabProps> = ({
  person
}) => {

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

  return (
    <div className={styles.container}>
      <div className={styles.personnelDetails}>
        <div className={styles.personPhoto}>
          <PersonPhoto hideTooltip={true} person={personnelDetails} size={'xlarge'} />
        </div>
        <div>
          <div className={styles.row}>
            {createField('First name', person.firstName || '')}
            {createField('Last name', person.lastName || '')}
          </div>
          <div className={styles.row}>
            {createField('Phone', person.phoneNumber || '')}
            {createField('E-Mail', person.mail || '')}
            {createField('AD Status', AzureAdStatusIcon(person.azureAdStatus || 'NoAccount'), 'AdStatus')}
          </div>

        </div>
      </div>
      <div className={styles.disciplines}>
        <h4>Disciplines</h4>
        <div className={styles.disciplinesChips}>
          {person.disciplines.map(d => <div key={d.name} className={styles.disciplineChip}>{d.name}</div>)}
        </div>
      </div>
    </div>
  );
};

export default GeneralTab;
