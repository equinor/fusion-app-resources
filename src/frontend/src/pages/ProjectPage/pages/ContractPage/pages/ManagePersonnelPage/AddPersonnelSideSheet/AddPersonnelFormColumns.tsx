import * as React from 'react';
import { TextInput } from '@equinor/fusion-components';
import Person from '../../../../../../../models/Person';
import * as styles from './styles.less'

export type DataItemProps = {
  item: Person;
  rowIndex: number;
};


export type TextInputColumnProps = {
  item: Person;
  onChange: (changedPerson: Person) => void
  field: keyof Person ;
};

const TextInputColumn : React.FC<TextInputColumnProps> = ({item,onChange,field}) => {
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

export type AddPersonnelFormColumnsProps = {
  personnel: Person[];
  setPersonnel: (changedPerson: Person[]) => void

};

const AddPersonnelFormColumns : React.FC<AddPersonnelFormColumnsProps> = ({personnel, setPersonnel}) => {
  
  const onChange = React.useCallback((changedPerson: Person)=>{
    const updatedPersons = personnel.map((p) => p.personnelId === changedPerson.personnelId ? changedPerson:p);
    setPersonnel(updatedPersons);
  },[personnel,setPersonnel])
  
  return (
  <>
    {
    personnel.map((person) => (
      <tr style={{display:"flex"}}>
        <td style={{flexGrow:1}}><TextInputColumn key={`name${person.personnelId}`} item={person} onChange={onChange} field={"name"}/></td>
        <td style={{flexGrow:1}}><TextInputColumn key={`mail${person.personnelId}`} item={person} onChange={onChange} field={"mail"}/></td>
        <td style={{flexGrow:1}}><TextInputColumn key={`phoneNumber${person.personnelId}`} item={person} onChange={onChange} field={"phoneNumber"}/></td>
      </tr>
    ))
    }
  </>
  )
}

export default AddPersonnelFormColumns
