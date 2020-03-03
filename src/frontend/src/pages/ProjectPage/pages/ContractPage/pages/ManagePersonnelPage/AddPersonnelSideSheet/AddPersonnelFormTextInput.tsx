import * as React from 'react';
import { TextInput } from '@equinor/fusion-components';
import Person from '../../../../../../../models/Person';

export type PersonnelFormTextInputProps = {
  item: Person;
  onChange: (changedPerson: Person) => void
  field: keyof Person ;
};

const AddPersonnelFormTextInput : React.FC<PersonnelFormTextInputProps> = ({item,onChange,field}) => {
    return (
      <TextInput 
        key = {field+item.personnelId}
        onChange = {(newValue) => {
          const changedPerson = {...item,[field]:newValue};
          onChange(changedPerson);
        }}
        value={item[field]}
      />
  )
}



export default AddPersonnelFormTextInput
